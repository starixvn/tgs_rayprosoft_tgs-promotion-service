using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.Promotion.Models.PromotionBucket;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices.Promotion;

public class
	CreatePromotionBonusDailyQueryHandler : IGetService<CreatePromotionBonusDailyRequest, MessageResponse>
{
	private readonly ITgsService<BrandGame> _brandGameService;
	private readonly ICashServiceClient _cashServiceClient;
	private readonly ILogger<CreatePromotionBonusDailyQueryHandler> _logger;
	private readonly IPlayerBucketRepository _playerBucketService;
	private readonly ITgsService<PlayerGameRebateAggregate> _playerGameRebateAggregateService;
	private readonly ITgsService<Player> _playerService;
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly ITgsService<PromotionBonus> _promotionBonusServices;
	private readonly IPromotionRepository _promotionRepository;
	private readonly ITgsService<PromotionRow> _promotionService;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITransactionRepository _transactionRepository;
	private readonly ITgsService<Transaction> _transactionService;

	public CreatePromotionBonusDailyQueryHandler(
		IPromotionBonusRepository promotionBonusFactory,
		IPlayerBucketRepository playerBucketService,
		ICashServiceClient cashServiceClient,
		ITransactionRepository transactionRepository,
		IPromotionRepository promotionRepository,
		ITgsService<PromotionRow> promotionService,
		ITgsService<PromotionBonus> promotionBonusService,
		ITgsService<PlayerGameRebateAggregate> playerGameRebateAggregateService,
		ITgsService<BrandGame> brandGameService,
		ITgsService<Player> playerService,
		ITgsService<Transaction> transactionService,
		ISerialNumberGenerator serialNumberGenerator,
		ILogger<CreatePromotionBonusDailyQueryHandler> logger)
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
		_serialNumberGenerator =
			serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
			.Where(x => x.From <= reportDate.UtcDateTime && x.To >= reportDate.UtcDateTime)
			.ToArrayAsync(cancellationToken);

		var playerGameRoundAggregate = await _playerGameRebateAggregateService.GetQueries()
			.Where(x => x.ReportDate == reportDate.UtcDateTime)
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
			.GroupBy(x => new
			{
				x.aggregate.BrandId,
				x.aggregate.PlayerCode,
				x.aggregate.GameType,
				x.aggregate.GameProviderId,
				x.aggregate.VipLevel,
				x.aggregate.ReportDate
			}, (player, list) => new PlayerWinLossDaily
			{
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel,
				GameProviderId = player.GameProviderId,
				ReportDate = player.ReportDate,
				WinLoss = list.Sum(l => l.aggregate.WinLoss),
				Turnover = list.Sum(l => l.aggregate.Turnover)
			})
			.ToArrayAsync(cancellationToken);

		var promotionBonus = await promotions.ToAsyncEnumerable()
			.WhereAwait(async x => !await _promotionBonusServices.GetQueries()
				.AnyAsync(
					pb => pb.PromotionId == x.Id
						&& pb.From == reportDate.UtcDateTime
						&& pb.To == reportDate.UtcDateTime,
					cancellationToken))
			.SelectAwait(async x =>
			{
				var id = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);

				var (players, amountUsed) =
					GetPromotionBonusPlayers(id, x, playerGameRoundAggregate);
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
			})
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

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

		var playerBonuses = bonus.SelectMany(
				x => x.PromotionPlayers.Join(
						players,
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
					}))
			.ToArray();

		await _playerBucketService.CreatePlayerBucketsAsync(playerBonuses.Select(x =>
			new CreatePlayerBucketRequest(x.BrandId,
				x.PlayerCode,
				x.DefaultWalletId,
				x.BucketType,
				x.BucketAmount)).ToArray()).ConfigureAwait(false);

		var transactionPromotions = new List<TransactionPromotion>();

		foreach (var playerBonus in playerBonuses)
		{
			await Policy.Handle<HttpRequestException>()
				.WaitAndRetryAsync(
					3,
					retryAttempt => TimeSpan.FromSeconds(1),
					(exception, time, retryCount, context) => _logger.LogError(
						"promotion daily externalId:{externalId} retryCount:{retryCount} Exception:{exception}",
						playerBonus.Id.ToString(),
						retryCount,
						exception))
				.ExecuteAsync(async () =>
				{
					if (!await _transactionService.GetQueries()
						.AnyAsync(
							x => x.PlayerCode == playerBonus.PlayerCode 
								&& x.ExternalTransactionId == playerBonus.Id.ToString(),
							cancellationToken))
					{
						var response = await _cashServiceClient
							.PromotionAsync(
								playerBonus.DefaultWalletId,
								playerBonus.BonusAmount,
								"promotion daily",
								playerBonus.Id.ToString(),
								null,
								cancellationToken)
							.ConfigureAwait(false);

						transactionPromotions.Add(new()
						{
							TransactionId = response.TransactionId,
							PromotionId = playerBonus.PromotionId,
							PromotionBonusId = playerBonus.PromotionBonusId,
							PromotionType = playerBonus.PromotionType
						});
					}
				}).ConfigureAwait(false);
		}

		await _transactionRepository.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());

		return new() { Valid = true };
	}

	private static (PromotionBonusPlayerModel[], decimal) GetPromotionBonusPlayers(
		Guid id,
		PromotionRow promotion,
		IEnumerable<PlayerWinLossDaily> aggregates)
	{
		var amount = promotion.AmountUsed;

		var promotionLevels = promotion.PromotionVipLevels.Select(v => v.Level).ToArray();

		var players = aggregates
			.Where(promotionLevels.Length > 0,
				pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
			.Where(promotion.GameType is not null, pa => pa.GameType == promotion.GameType)
			.Where(promotion.PromotionGameProviders.Any(), pa =>
				promotion.PromotionGameProviders.Select(pg => pg.GameProviderId)
					.Contains(pa.GameProviderId)).ToArray();

		return promotion.PromotionType switch
		{
			"TurnoverRebate" => (promotion.PromotionParticipants.Where(x =>
					x.CreatedOn >= promotion.From && x.CreatedOn <= promotion.To).Select(p =>
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
			"ReliefMoneyDaily" => (promotion.PromotionParticipants.Where(x =>
					x.CreatedOn >= promotion.From && x.CreatedOn <= promotion.To).Select(x => x.PlayerCode).Distinct().Select(playerCode =>
				{
					if (amount > promotion.TotalAmountLimit && promotion.Unlimited == 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = playerCode!,
							BonusAmount = 0M,
							PromotionType = promotion.PromotionType
						};
					}

					var playerRebateAmount = players.Where(pa => pa.PlayerCode == playerCode)
						.Sum(s => s.WinLoss) * promotion.Percentage / 100;

					if (playerRebateAmount <= 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = playerCode!,
							BonusAmount = 0,
							PromotionType = promotion.PromotionType
						};
					}

					playerRebateAmount = playerRebateAmount > promotion.BonusAmountLimit
						? promotion.BonusAmountLimit
						: playerRebateAmount;

					amount += playerRebateAmount;

					return new PromotionBonusPlayerModel
					{
						PromotionBonusId = id,
						PlayerCode = playerCode!,
						BonusAmount = playerRebateAmount,
						PromotionType = promotion.PromotionType
					};
				})
				.ToArray(), amount),
			_ => (Array.Empty<PromotionBonusPlayerModel>(), 0)
		};
	}
}
