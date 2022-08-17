using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.Promotion.Models.PromotionBucket;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.ApplicationServices.Promotion;
internal class TurnoverRebateWeeklyDistribution : IDistribution
{
	private PromotionRow _promotion;
	private IEnumerable<PlayerTurnoverWeekly> _playerWinLossWeekly = Array.Empty<PlayerTurnoverWeekly>();
	private PromotionBonusModel _promotionBonusModel;
	private DateTimeOffset _previousMonday = new DateTimeOffset(DateTimeOffset.Now.Year,
		DateTimeOffset.Now.Month,
		DateTimeOffset.Now.Day,
		0,
		0,
		0,
		new(8, 0, 0)).AddDays(-7).GetMonday();
	private DateTimeOffset _previousSunday = new DateTimeOffset(DateTimeOffset.Now.Year,
		DateTimeOffset.Now.Month,
		DateTimeOffset.Now.Day,
		0,
		0,
		0,
		new(8, 0, 0)).AddDays(-7).GetSunday();

	private readonly ITgsService<BrandGame> _brandGameService;
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketRepository _playerBucketService;
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly IPromotionRepository _promotionRepository;
	private readonly ITgsService<PlayerGameRebateAggregate> _playerGameRebateAggregateService;
	private readonly ITgsService<Player> _playerService;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITransactionRepository _transactionRepository;
	private readonly ITgsService<Transaction> _transactionService;
	private readonly PromotionSerializer _promotionSerializer;
	private readonly ILogger<CreatePromotionBonusWeeklyHandler> _logger;

	public TurnoverRebateWeeklyDistribution(
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
		PromotionSerializer promotionSerializer,
		ILogger<CreatePromotionBonusWeeklyHandler> logger)
	{
		_promotionBonusFactory = promotionBonusFactory;
		_playerBucketService = playerBucketService;
		_cashServiceClient = cashServiceClient;
		_playerGameRebateAggregateService = playerGameRebateAggregateService;
		_playerService = playerService;
		_promotionRepository = promotionRepository;
		_transactionRepository = transactionRepository;
		_serialNumberGenerator = serialNumberGenerator;
		_brandGameService = brandGameService;
		_transactionService = transactionService;
		_promotionSerializer = promotionSerializer;
		_logger = logger;
	}

	public IDistribution SetPromotion(PromotionRow  promotion,DateTimeOffset? from,DateTimeOffset? to)
	{
		_promotion = promotion;
		_previousMonday = from ?? _previousMonday;
		_previousSunday = to ?? _previousSunday;

		if (_promotion == null)
			throw new ArgumentNullException(nameof(promotion));

		return this;
	}

	public async ValueTask<IDistribution> SetConditionDataAsync(CancellationToken cancellationToken)
	{
		_playerWinLossWeekly = await _playerGameRebateAggregateService.GetQueries()
			.Where(x => x.BrandId == _promotion.BrandId)
			.Where(x => x.ReportDate >= _previousMonday.UtcDateTime && x.ReportDate <= _previousSunday.UtcDateTime)
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
			}, (player, list) => new PlayerTurnoverWeekly
			{
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel,
				GameProviderId = player.GameProviderId,
				ReportDate = player.ReportDate,
				Turnover = list.Sum(l => l.aggregate.Turnover)
			})
			.ToArrayAsync(cancellationToken);

		return this;
	}

	public async ValueTask<IDistribution> CalculateBonusAmountAsync(CancellationToken cancellationToken)
	{
		var id = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);
		var from = _promotion.From > _previousMonday ? _promotion.From : _previousMonday;
		var to = _promotion.To < _previousSunday ? _promotion.To : _previousSunday;

		var amount = _promotion.AmountUsed;

		var promotionLevels = _promotion.PromotionVipLevels.Select(v => v.Level).ToArray();

		var players = _playerWinLossWeekly
			.Where(promotionLevels.Length > 0,
				pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
			.Where(_promotion.GameType is not null, pa => pa.GameType == _promotion.GameType)
			.Where(_promotion.PromotionGameProviders.Any(), pa =>
				_promotion.PromotionGameProviders.Select(pg => pg.GameProviderId)
					.Contains(pa.GameProviderId))
			.Where(pa => pa.ReportDate >= from)
			.Where(pa => pa.ReportDate <= to);

		var rule = _promotionSerializer.Deserialize<TurnoverRebateWeeklyRule>(_promotion.CampaignDetail)!;

		var playerBonus = _promotion.PromotionParticipants.Where(x =>
			x.CreatedOn >= _promotion.From && x.CreatedOn <= _promotion.To).Select(x => x.PlayerCode).Distinct().Select(playerCode =>
		{
			var playerTurnover = players.Where(pa => pa.PlayerCode == playerCode).Sum(s => s.Turnover);

			if (amount > _promotion.TotalAmountLimit && _promotion.Unlimited == 0 || playerTurnover < rule.TurnoverLimit)
			{
				return new PromotionBonusPlayerModel
				{
					PromotionBonusId = id,
					PlayerCode = playerCode!,
					BonusAmount = 0M,
					PromotionType = _promotion.PromotionType
				};
			}

			var playerRebateAmount = playerTurnover * rule.Percentage / 100;

			playerRebateAmount = playerRebateAmount > rule.BonusAmountLimit
				? rule.BonusAmountLimit
				: playerRebateAmount;

			amount += playerRebateAmount;

			return new PromotionBonusPlayerModel
			{
				PromotionBonusId = id,
				PlayerCode = playerCode!,
				BonusAmount = playerRebateAmount,
				PromotionType = _promotion.PromotionType
			};
		}).ToArray();

		_promotion.AmountUsed = amount;

		var promotionData = new
		{
			_promotion.Id,
			_promotion.BrandId,
			_promotion.From,
			_promotion.To,
			Status = (PromotionStatus)_promotion.Status,
			_promotion.Name,
			_promotion.AutoSentPlayerBonus,
			_promotion.PromotionType,
			_promotion.BonusAmountLimit,
			_promotion.AmountUsed,
			Unlimited = _promotion.Unlimited == 1,
			GameType = (GameType?)_promotion.GameType,
			_promotion.Percentage,
			_promotion.TotalAmountLimit,
			_promotion.TurnoverMultiple,
			GameProviderIds = _promotion.PromotionGameProviders.Select(g => g.GameProviderId).ToArray(),
			VipLevels = _promotion.PromotionVipLevels.Select(v => v.Level).OrderBy(level => level).ToArray(),
			_promotion.CreatedOn,
			_promotion.UpdatedOn,
			_promotion.CreatedByUsername,
			_promotion.UpdatedByUsername,
			_promotion.SubSubject,
			_promotion.Content,
			_promotion.Terms,
			_promotion.DesktopBanner,
			_promotion.MobileBanner,
			_promotion.Localization,
			Number = _promotion.DisplayOrder,
			_promotion.CampaignDetail
		};

		_promotionBonusModel = new PromotionBonusModel
		{
			Id = id,
			BrandId = _promotion.BrandId,
			PromotionId = _promotion.Id,
			From = from,
			To = to,
			AutoSentPlayerBonus = _promotion.AutoSentPlayerBonus,
			PromotionType = _promotion.PromotionType,
			TotalAmountLimit = _promotion.TotalAmountLimit,
			GameType = (GameType?)_promotion.GameType,
			Percentage = rule.Percentage,
			BonusAmountLimit = rule.BonusAmountLimit,
			TurnoverMultiple = rule.TurnoverMultiple,
			CreatedOn = DateTimeOffset.Now,
			SentOn = _promotion.AutoSentPlayerBonus ? DateTimeOffset.Now : null,
			Players = playerBonus.Where(p => p.BonusAmount > 0M).ToArray(),
			Detail = JsonSerializer.Serialize(promotionData,new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				ReferenceHandler = ReferenceHandler.IgnoreCycles,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			})
		};

		return this;
	}

	public async ValueTask<IDistribution> DistributeBonusAsync(CancellationToken cancellationToken)
	{
		await _promotionRepository.UpdatePromotionsAsync(new[] { _promotion! });
		var bonus = await _promotionBonusFactory.CreatePromotionBonusAsync(new[] { _promotionBonusModel });

		if (_promotion.AutoSentPlayerBonus)
		{
			var playerCodes = bonus.SelectMany(p => p.PromotionPlayers).Select(p => p.PlayerCode);

			var players = await _playerService.GetQueries()
				.Where(x => playerCodes.Contains(x.PlayerCode))
				.Select(x => new
				{
					x.BrandId,
					x.PlayerCode,
					x.DefaultWalletId
				})
				.ToArrayAsync(cancellationToken);

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
									"promotion turnover rebate weekly",
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
		}

		return this;
	}}
