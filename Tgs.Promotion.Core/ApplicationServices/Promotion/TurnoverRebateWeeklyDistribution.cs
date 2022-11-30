using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.ApplicationServices.Promotion;

internal class TurnoverRebateWeeklyDistribution : IDistribution
{
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly ICashServiceClient _cashServiceClient;
	private readonly ILogger<CreatePromotionBonusWeeklyHandler> _logger;
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly IPromotionRepository _promotionRepository;
	private readonly PromotionSerializer _promotionSerializer;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly TgsContext _tgsContext;
	private readonly ITransactionRepository _transactionRepository;
	private IEnumerable<PlayerTurnoverWeekly> _playerWinLossWeekly = Array.Empty<PlayerTurnoverWeekly>();

	private DateTimeOffset _previousMonday = new DateTimeOffset(
		DateTimeOffset.Now.Year,
		DateTimeOffset.Now.Month,
		DateTimeOffset.Now.Day,
		0,
		0,
		0,
		new(8, 0, 0)).AddDays(-7).GetMonday();

	private DateTimeOffset _previousSunday = new DateTimeOffset(
		DateTimeOffset.Now.Year,
		DateTimeOffset.Now.Month,
		DateTimeOffset.Now.Day,
		0,
		0,
		0,
		new(8, 0, 0)).AddDays(-7).GetSunday();

	private PromotionRow? _promotion;
	private PromotionBonusModel? _promotionBonusModel;

	public TurnoverRebateWeeklyDistribution(
		IPlayerBucketClient playerBucketClient,
		ICashServiceClient cashServiceClient,
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusFactory,
		ITransactionRepository transactionRepository,
		ISerialNumberGenerator serialNumberGenerator,
		TgsContext tgsContext,
		PromotionSerializer promotionSerializer,
		ILogger<CreatePromotionBonusWeeklyHandler> logger)
	{
		_promotionBonusFactory = promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<IDistribution> CalculateBonusAmountAsync(CancellationToken cancellationToken)
	{
		var id = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);

		var amount = _promotion!.AmountUsed;

		var rule = _promotionSerializer.Deserialize<TurnoverRebateWeeklyRule>(_promotion.CampaignDetail)!;

		var playerBonus = _playerWinLossWeekly.GroupBy(x => x.PlayerCode,
			(playerCode, list) => new
			{
				PlayerCode = playerCode,
				Turnover = list.Sum(s => s.Turnover)
			}).Select(x =>
		{
			var playerTurnover = x.Turnover;

			if (amount > _promotion.TotalAmountLimit && _promotion.Unlimited == 0 ||
				playerTurnover < rule.TurnoverLimit)
			{
				return new PromotionBonusPlayerModel
				{
					PromotionBonusId = id,
					PlayerCode = x.PlayerCode,
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
				PlayerCode = x.PlayerCode,
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
			GameProviderIds = rule.GameProviderIds?.ToArray(),
			VipLevels =
				_tgsContext.PromotionVipLevels.Where(x => x.PromotionId == _promotion.Id).Select(v => v.Level)
					.ToArray(),
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

		_promotionBonusModel = new()
		{
			Id = id,
			BrandId = _promotion.BrandId,
			PromotionId = _promotion.Id,
			From = _previousMonday,
			To = _previousSunday,
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
			Detail = JsonSerializer.Serialize(promotionData,
				new JsonSerializerOptions
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
		await _promotionRepository.UpdatePromotionsAsync(new[]
		{
			_promotion!
		});

		var bonus = await _promotionBonusFactory.CreatePromotionBonusAsync(new[] { _promotionBonusModel! });

		if (_promotion!.AutoSentPlayerBonus)
		{
			var playerCodes = bonus.SelectMany(p => p.PromotionPlayers).Select(p => p.PlayerCode);

			var players = await _tgsContext.Players
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

			_ = await Task.WhenAll(playerBonuses.Select(pb => Task.Run(async () => await _playerBucketClient.CreateBucketAsync(
				pb.PlayerCode,
				pb.BucketAmount,
				pb.BucketType,
				pb.Id.ToString(),
				cancellationToken).ConfigureAwait(false)))).ConfigureAwait(false);

			var transactionPromotions = new List<TransactionPromotion>();

			foreach (var playerBonus in playerBonuses)
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
						if (!await _tgsContext.Transactions
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

			await _transactionRepository.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());
		}

		return this;
	}

	public async ValueTask<IDistribution> SetConditionDataAsync(CancellationToken cancellationToken)
	{
		var promotionLevels = _tgsContext.PromotionVipLevels.Where(x => x.PromotionId == _promotion!.Id)
			.Select(v => v.Level).ToArray();

		var rule = _promotionSerializer.Deserialize<TurnoverRebateWeeklyRule>(_promotion!.CampaignDetail)!;

		var playerWinLossWeeklies = _tgsContext.PlayerGameRebateAggregates
			.Where(x => x.BrandId == _promotion.BrandId)
			.Where(x => x.ReportDate >= _previousMonday.UtcDateTime && x.ReportDate <= _previousSunday.UtcDateTime)
			.Where(promotionLevels.Length > 0,
				pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
			.Where(rule.GameType is not null, pa => pa.GameType == (short)rule.GameType!)
			.Where(rule.GameProviderIds?.Any(), pa =>
				rule.GameProviderIds!.Contains(pa.GameProviderId))
			.GroupJoin(_tgsContext.BrandGames,
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
			});

		if (CheckIsAutoJoin(_promotion.CampaignDetail))
		{
			_playerWinLossWeekly = await playerWinLossWeeklies.ToArrayAsync(cancellationToken).ConfigureAwait(false);
		}
		else
		{
			_playerWinLossWeekly = await playerWinLossWeeklies
				.Join(_tgsContext.PromotionParticipants.Where(pp => pp.PromotionId == _promotion.Id).Where(x =>
						x.CreatedOn >= _promotion.From && x.CreatedOn <= _promotion.To).Select(x => x.PlayerCode)
					.Distinct(), agg => agg.PlayerCode, playerCode => playerCode, (agg, playerCode) => new
				{
					agg.BrandId,
					agg.ReportDate,
					agg.PlayerCode,
					agg.GameType,
					agg.GameProviderId,
					agg.VipLevel,
					agg.Turnover
				})
				.Select(x => new PlayerTurnoverWeekly
				{
					BrandId = x.BrandId,
					PlayerCode = x.PlayerCode,
					GameType = x.GameType,
					VipLevel = x.VipLevel,
					GameProviderId = x.GameProviderId,
					ReportDate = x.ReportDate,
					Turnover = x.Turnover
				})
				.ToArrayAsync(cancellationToken).ConfigureAwait(false);
		}

		return this;
	}

	public IDistribution SetPromotion(PromotionRow promotion, DateTimeOffset? from, DateTimeOffset? to)
	{
		_promotion = promotion;
		_previousMonday = from ?? (_promotion.From > _previousMonday ? _promotion.From : _previousMonday);
		_previousSunday = to ?? (_promotion.To < _previousSunday ? _promotion.To : _previousSunday);

		return _promotion == null ? throw new ArgumentNullException(nameof(promotion)) : (IDistribution)this;
	}

	private static bool CheckIsAutoJoin(string campaignDetail)
	{
		return JsonSerializer.Deserialize<PlayerAutoJoinModel>(campaignDetail, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.PlayerAutoJoin ?? false;
	}
}
