using System.Text.Json;
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

public class CreatePromotionBonusWeeklyHandler
	: IGetService<CreatePromotionBonusWeeklyRequest, MessageResponse>
{
	private readonly ITgsService<BrandGame> _brandGameService;
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketRepository _playerBucketService;
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly ITgsService<PromotionBonus> _promotionBonusService;
	private readonly IPromotionRepository _promotionRepository;
	private readonly ITgsService<PlayerGameRebateAggregate> _playerGameRebateAggregateService;
	private readonly ITgsService<Player> _playerService;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITransactionRepository _transactionRepository;
	private readonly ITgsService<Transaction> _transactionService;
	private readonly ILogger<CreatePromotionBonusWeeklyHandler> _logger;

	public CreatePromotionBonusWeeklyHandler(
		ITgsService<PromotionBonus> promotionBonusService,
		IPlayerBucketRepository playerBucketService,
		ICashServiceClient cashServiceClient,
		ITgsService<PlayerGameRebateAggregate> playerGameRebateAggregateService,
		ITgsService<Player> playerService,
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusFactory,
		ITransactionRepository transactionRepository,
		ISerialNumberGenerator serialNumberGenerator,
		ITgsService<Transaction> transactionService,
		ITgsService<BrandGame> brandGameService,
		ILogger<CreatePromotionBonusWeeklyHandler> logger)
	{
		_promotionBonusFactory =
			promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
		_promotionBonusService =
			promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
		_playerBucketService = playerBucketService ?? throw new ArgumentNullException(nameof(playerBucketService));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerGameRebateAggregateService = playerGameRebateAggregateService ?? throw new ArgumentNullException(nameof(playerGameRebateAggregateService));
		_playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_transactionRepository =
			transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_brandGameService = brandGameService ?? throw new ArgumentNullException(nameof(brandGameService));
		_transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionBonusWeeklyRequest request,
		CancellationToken cancellationToken)
	{
		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			new(8, 0, 0));

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		if (!string.IsNullOrEmpty(request.From) &&
			DateTimeOffset.TryParse($"{request.From} 00:00 +08:00", out var resultFrom))
		{
			previousMonday = new(resultFrom.Year,
				resultFrom.Month,
				resultFrom.Day,
				0,
				0,
				0,
				resultFrom.Offset);
		}

		if (!string.IsNullOrEmpty(request.To) &&
			DateTimeOffset.TryParse($"{request.From} 00:00 +08:00", out var resultTo))
		{
			previousSunday = new(resultTo.Year,
				resultTo.Month,
				resultTo.Day,
				0,
				0,
				0,
				resultTo.Offset);
		}

		var promotions = await _promotionRepository.GetPromotionQueries()
			.Include(x => x.PromotionGameProviders)
			.Include(x => x.PromotionVipLevels)
			.Include(x => x.PromotionParticipants)
			.Where(x => x.Status == (short)PromotionStatus.Enabled)
			.Where(x => x.SettlementIntervalType == "Weekly")
			.Where(x => x.From >= previousMonday.UtcDateTime && x.From <= previousSunday.UtcDateTime ||
				x.To >= previousMonday.UtcDateTime && x.To <= previousSunday.UtcDateTime ||
				x.From < previousMonday.UtcDateTime && x.To > previousSunday.UtcDateTime
			).ToArrayAsync(cancellationToken);

		var playerGameRoundAggregate = await _playerGameRebateAggregateService.GetQueries()
			.Where(x => x.ReportDate >= previousMonday.UtcDateTime && x.ReportDate <= previousSunday.UtcDateTime)
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
			}, (player, list) => new PlayerWinLossWeekly
			{
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel,
				GameProviderId = player.GameProviderId,
				ReportDate = player.ReportDate,
				WinLoss = list.Sum(l => l.aggregate.WinLoss)
			})
			.ToArrayAsync(cancellationToken);

		var promotionBonus = await promotions.ToAsyncEnumerable()
			.WhereAwait(async x =>
			{
				var from = x.From > previousMonday ? x.From : previousMonday;
				var to = x.To < previousSunday ? x.To : previousSunday;

				return !await _promotionBonusService.GetQueries()
					.AnyAsync(
						pb => pb.PromotionId == x.Id && pb.From == from.UtcDateTime &&
							pb.To == to.UtcDateTime,
						cancellationToken);
			})
			.SelectAwait(async x =>
			{
				var id = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);
				var from = x.From > previousMonday ? x.From : previousMonday;
				var to = x.To < previousSunday ? x.To : previousSunday;

				var (players, amountUsed) = GetPromotionBonusPlayers(
					id,
					x,
					playerGameRoundAggregate,
					from,
					to);
				x.AmountUsed = amountUsed;

				return new PromotionBonusModel
				{
					Id = id,
					BrandId = x.BrandId,
					PromotionId = x.Id,
					From = from,
					To = to,
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
			.ToArrayAsync(cancellationToken: cancellationToken)
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

		await _promotionRepository.UpdatePromotionsAsync(promotions);
		var bonus = await _promotionBonusFactory.CreatePromotionBonusAsync(promotionBonus);

		var playerBonuses = bonus
			.SelectMany(x => x.PromotionPlayers
				.Join(
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
						"promotion weekly externalId:{externalId} retryCount:{retryCount} Exception:{exception}",
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
								"promotion weekly",
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
				});
		}

		await _transactionRepository.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());

		return new MessageResponse { Valid = true };
	}

	private static (PromotionBonusPlayerModel[], decimal) GetPromotionBonusPlayers(
		Guid id,
		PromotionRow promotion,
		IEnumerable<PlayerWinLossWeekly> aggregates,
		DateTimeOffset from,
		DateTimeOffset to)
	{
		var amount = promotion.AmountUsed;

		var promotionLevels = promotion.PromotionVipLevels.Select(v => v.Level).ToArray();

		var players = aggregates
			.Where(promotionLevels.Length > 0,
				pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
			.Where(promotion.GameType is not null, pa => pa.GameType == promotion.GameType)
			.Where(promotion.PromotionGameProviders.Any(), pa =>
				promotion.PromotionGameProviders.Select(pg => pg.GameProviderId)
					.Contains(pa.GameProviderId))
			.Where(pa => pa.ReportDate >= from)
			.Where(pa => pa.ReportDate <= to);

		return promotion.PromotionType switch
		{
			"ReliefMoneyWeekly" => (promotion.PromotionParticipants.Where(x =>
					x.CreatedOn >= promotion.From && x.CreatedOn <= promotion.To).Select(p =>
			{
				if (amount > promotion.TotalAmountLimit && promotion.Unlimited == 0)
				{
					return new PromotionBonusPlayerModel
					{
						PromotionBonusId = id,
						PlayerCode = p.PlayerCode,
						BonusAmount = 0M,
						PromotionType = promotion.PromotionType
					};
				}

				var playerRebateAmount =
					players.Where(pa => pa.PlayerCode == p.PlayerCode).Sum(s => s.WinLoss) *
					promotion.Percentage / 100;

				if (playerRebateAmount <= 0)
				{
					return new PromotionBonusPlayerModel
					{
						PromotionBonusId = id,
						PlayerCode = p.PlayerCode,
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
					PlayerCode = p.PlayerCode,
					BonusAmount = playerRebateAmount,
					PromotionType = promotion.PromotionType
				};
			})
				.ToArray(), amount),
			_ => (Array.Empty<PromotionBonusPlayerModel>(), 0)
		};
	}
}
