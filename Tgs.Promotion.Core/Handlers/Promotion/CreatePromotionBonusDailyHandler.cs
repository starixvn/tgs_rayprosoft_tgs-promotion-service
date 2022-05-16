using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.Promotion.Core.Extensions;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.PromotionBonus;
using Tgs.Promotion.Domain.Models.PromotionBucket;
using Tgs.Promotion.Domain.shared;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Handlers.Promotion;

public class
	CreatePromotionBonusDailyQueryHandler : IGetService<CreatePromotionBonusDailyRequest, MessageResponse>
{
	private readonly ITgsService<BrandGame> _brandGameService;
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketRepository _playerBucketService;
	private readonly ITgsService<PlayerGameRebateAggregate> _playerGameRebateAggregateService;
	private readonly ITgsService<Player> _playerService;
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly ITgsService<PromotionBonus> _promotionBonusServices;
	private readonly IPromotionRepository _promotionRepository;
	private readonly ITgsService<Domain.Entities.Promotion> _promotionService;
	private readonly ITransactionRepository _transactionRepository;

	public CreatePromotionBonusDailyQueryHandler(
		IPromotionBonusRepository promotionBonusFactory,
		IPlayerBucketRepository playerBucketService,
		ICashServiceClient cashServiceClient,
		ITransactionRepository transactionRepository,
		IPromotionRepository promotionRepository,
		ITgsService<Domain.Entities.Promotion> promotionService,
		ITgsService<PromotionBonus> promotionBonusService,
		ITgsService<PlayerGameRebateAggregate> playerGameRebateAggregateService,
		ITgsService<BrandGame> brandGameService,
		ITgsService<Player> playerService)
	{
		_promotionBonusFactory =
			promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
		_playerBucketService = playerBucketService ?? throw new ArgumentNullException(nameof(playerBucketService));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_transactionRepository =
			transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
		_promotionService =
			promotionService ?? throw new ArgumentNullException(nameof(promotionService));
		_promotionBonusServices =
			promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
		_playerGameRebateAggregateService =
			playerGameRebateAggregateService ??
			throw new ArgumentNullException(nameof(playerGameRebateAggregateService));
		_brandGameService =
			brandGameService ?? throw new ArgumentNullException(nameof(brandGameService));
		_playerService =
			playerService ?? throw new ArgumentNullException(nameof(playerService));
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionBonusDailyRequest request,
		CancellationToken cancellationToken)
	{
		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		if (!string.IsNullOrEmpty(request.Date) &&
			DateTimeOffset.TryParse($"{request.Date} 00:00 +08:00", out var result))
		{
			reportDate = new(result.Year,
				result.Month,
				result.Day,
				0,
				0,
				0,
				result.Offset);
		}

		var promotions = await _promotionService.GetQueries()
			.Include(x => x.PromotionGameProviders)
			.Include(x => x.PromotionVipLevels)
			.Include(x => x.PromotionParticipants)
			.Where(x => x.Status == (short)PromotionStatus.Enabled)
			.Where(x => x.SettlementIntervalType == "Daily")
			.Where(x => x.From <= reportDate.UtcDateTime && x.To >= reportDate.UtcDateTime).ToArrayAsync(cancellationToken);

		var promotionIds = promotions.Select(x => x.Id);

		if (await _promotionBonusServices.GetQueries()
				.AnyAsync(
					x => promotionIds.Contains(x.PromotionId) && reportDate.UtcDateTime == x.From &&
						reportDate.UtcDateTime == x.To,
					cancellationToken))
		{
			return new()
			{
				Valid = false,
				Message = $"Promotion bonus report date {reportDate} is already existed."
			};
		}

		var gameProviderIds = promotions.SelectMany(x => x.PromotionGameProviders).Select(x => x.GameProviderId)
			.ToArray();

		var gameTypes = promotions.Select(x => x.GameType).ToArray();

		var playerGameRoundAggregate = await _playerGameRebateAggregateService.GetQueries()
			.Where(x => x.ReportDate == reportDate.UtcDateTime)
			.Where(x => gameProviderIds.Contains(x.GameProviderId))
			.Where(!gameTypes.Contains(null), x => gameTypes.Contains(x.GameType))
			.GroupJoin(_brandGameService.GetQueries(),
				aggregate => new
				{
					aggregate.BrandId,
					aggregate.GameId
				},
				brandGame => new
				{
					brandGame.BrandId,
					brandGame.GameId
				},
				(aggregate, brandGame) => new
				{
					aggregate,
					brandGame
				})
			.SelectMany(x => x.brandGame.DefaultIfEmpty(),
				(x, brandGame) => new
				{
					x.aggregate,
					brandGame
				})
			.Where(x => x.brandGame == null || x.brandGame.Rebate)
			.ToArrayAsync(cancellationToken);

		var promotionBonus = promotions.Select(x =>
		{
			var id = GuidExtensions.NewSequentialGuid();

			var (players, amountUsed) =
				GetPromotionBonusPlayers(id, x, playerGameRoundAggregate.Select(p => p.aggregate));
			x.AmountUsed = amountUsed;

			return new PromotionBonusModel
			{
				Id = id,
				BrandId = x.BrandId,
				PromotionId = x.Id,
				From = reportDate.UtcDateTime,
				To = reportDate.UtcDateTime,
				AutoSentPlayerBonus = x.AutoSentPlayerBonus,
				PromotionType = x.PromotionType,
				TotalAmountLimit = x.TotalAmountLimit,
				GameType = (GameType?)x.GameType,
				Percentage = x.Percentage,
				BonusAmountLimit = x.BonusAmountLimit,
				TurnoverMultiple = x.TurnoverMultiple,
				CreatedOn = DateTimeOffset.Now,
				SentOn = x.AutoSentPlayerBonus ? DateTimeOffset.Now : null,
				Players = players.Where(p => p.BonusAmount > 0M).ToArray()
			};
		}).ToArray();
		var autoSentPlayerBonuses = promotionBonus.Where(x => x.AutoSentPlayerBonus).ToArray();
		var playerCodes = autoSentPlayerBonuses.SelectMany(p => p.Players).Select(p => p.PlayerCode);

		var players = await _playerService.GetQueries()
			.Where(x => playerCodes.Contains(x.PlayerCode))
			.Select(x => new
			{
				x.BrandId,
				x.PlayerCode,
				x.DefaultWalletId
			})
			.ToArrayAsync(cancellationToken);

		await _promotionRepository.UpdatePromotionsAsync(promotions).ConfigureAwait(false);
		var bonus = await _promotionBonusFactory.CreatePromotionBonusAsync(promotionBonus).ConfigureAwait(false);

		var playerBuckets = bonus
			.SelectMany(x =>
			{
				return x.PromotionPlayers.Join(players,
						pb => pb.PlayerCode,
						p => p.PlayerCode,
						(pb, p) => new
						{
							pb,
							p
						})
					.Select(p => new
					{
						p.pb.Id,
						x.BrandId,
						p.p.PlayerCode,
						p.p.DefaultWalletId,
						BucketAmount = p.pb.BonusAmount * x.TurnoverMultiple,
						p.pb.BonusAmount,
						BucketType = BucketType.Promotion,
						x.PromotionId,
						PromotionBonusId = x.Id,
						x.PromotionType
					});
			}).ToArray();

		await _playerBucketService.CreatePlayerBucketsAsync(playerBuckets.Select(x =>
			new CreatePlayerBucketRequest(x.BrandId,
				x.PlayerCode,
				x.DefaultWalletId,
				x.BucketType,
				x.BucketAmount)).ToArray()).ConfigureAwait(false);

		var transactionPromotions = new List<TransactionPromotion>();

		foreach (var createPlayerBucketRequest in playerBuckets)
		{
			var response = await _cashServiceClient
				.PromotionAsync(
					createPlayerBucketRequest.DefaultWalletId,
					createPlayerBucketRequest.BonusAmount,
					null,
					createPlayerBucketRequest.Id.ToString(),
					null)
				.ConfigureAwait(false);

			transactionPromotions.Add(new()
			{
				TransactionId = response.TransactionId,
				PromotionId = createPlayerBucketRequest.PromotionId,
				PromotionBonusId = createPlayerBucketRequest.PromotionBonusId,
				PromotionType = createPlayerBucketRequest.PromotionType
			});
		}

		await _transactionRepository.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());

		return new()
		{ Valid = true };
	}

	private static (PromotionBonusPlayerModel[], decimal) GetPromotionBonusPlayers(
		Guid id,
		Domain.Entities.Promotion promotion, IEnumerable<PlayerGameRebateAggregate> aggregates)
	{
		var amount = promotion.AmountUsed;

		var promotionLevels = promotion.PromotionVipLevels.Select(v => v.Level).ToArray();

		var players = aggregates
			.Where(promotionLevels.Length > 0,
				pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
			.Where(promotion.GameType is not null, pa => pa.GameType == promotion.GameType)
			.Where(pa =>
				promotion.PromotionGameProviders.Select(pg => pg.GameProviderId)
					.Contains(pa.GameProviderId)).ToArray();

		return promotion.PromotionType switch
		{
			"TurnoverRebate" => (promotion.PromotionParticipants.Select(p =>
				{
					if (amount > promotion.TotalAmountLimit && promotion.Unlimited == 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = p.PlayerCode!,
							BonusAmount = 0M,
							PromotionType = promotion.PromotionType
						};
					}

					var playerRebateAmount =
						players.Where(pa => pa.PlayerCode == p.PlayerCode).Sum(s => s.Turnover) *
						promotion.Percentage / 100;

					playerRebateAmount = playerRebateAmount > promotion.BonusAmountLimit
						? promotion.BonusAmountLimit
						: playerRebateAmount;

					amount += playerRebateAmount;

					return new()
					{
						PromotionBonusId = id,
						PlayerCode = p.PlayerCode!,
						BonusAmount = playerRebateAmount,
						PromotionType = promotion.PromotionType
					};
				})
				.ToArray(), amount),
			"ReliefMoneyDaily" => (promotion.PromotionParticipants.Select(p =>
				{
					if (amount > promotion.TotalAmountLimit && promotion.Unlimited == 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = p.PlayerCode!,
							BonusAmount = 0M,
							PromotionType = promotion.PromotionType
						};
					}

					var playerRebateAmount = players.Where(pa => pa.PlayerCode == p.PlayerCode)
						.Sum(s => s.WinLoss) * promotion.Percentage / 100;

					if (playerRebateAmount <= 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = p.PlayerCode!,
							BonusAmount = 0,
							PromotionType = promotion.PromotionType
						};
					}

					playerRebateAmount = playerRebateAmount > promotion.BonusAmountLimit
						? promotion.BonusAmountLimit
						: playerRebateAmount;

					amount += playerRebateAmount;

					return new()
					{
						PromotionBonusId = id,
						PlayerCode = p.PlayerCode!,
						BonusAmount = playerRebateAmount,
						PromotionType = promotion.PromotionType
					};
				})
				.ToArray(), amount),
			_ => (Array.Empty<PromotionBonusPlayerModel>(), 0)
		};
	}
}
