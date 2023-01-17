#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.ApplicationServices.Promotion.Workers;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.ApplicationServices.Promotion;

internal class TurnoverRebateWeeklyDistribution : IDistribution
{
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly IPromotionRepository _promotionRepository;
	private readonly PromotionSerializer _promotionSerializer;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IDistributionTaskQueue _taskQueue;
	private readonly TgsContext _tgsContext;
	private IEnumerable<PlayerTurnoverWeekly> _playerWinLossWeekly = Array.Empty<PlayerTurnoverWeekly>();

	private DateTimeOffset _previousMonday;

	private DateTimeOffset _previousSunday;

	private PromotionRow? _promotion;
	private PromotionBonusModel? _promotionBonusModel;

	public TurnoverRebateWeeklyDistribution(
		IDistributionTaskQueue taskQueue,
		IPromotionRepository promotionRepository,
		IPromotionBonusRepository promotionBonusFactory,
		ISerialNumberGenerator serialNumberGenerator,
		TgsContext tgsContext,
		PromotionSerializer promotionSerializer)
	{
		_taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
		_promotionBonusFactory =
			promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_serialNumberGenerator =
			serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
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
			From = _previousMonday.UtcDateTime,
			To = _previousSunday.UtcDateTime,
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
		if (!await _tgsContext.PromotionBonuses.AnyAsync(x
				=> x.PromotionId == _promotionBonusModel!.PromotionId && x.From == _promotionBonusModel.From.UtcDateTime &&
				x.To == _promotionBonusModel.To.UtcDateTime, cancellationToken))
		{
			await _promotionRepository.UpdatePromotionAsync(_promotion!);

			_ = await _promotionBonusFactory.CreatePromotionBonusAsync(_promotionBonusModel!);
		}

		if (!_promotion!.AutoSentPlayerBonus)
		{
			return this;
		}

		var bonus = await _tgsContext.PromotionBonuses.FirstAsync(x
			=> x.PromotionId == _promotionBonusModel!.PromotionId && x.From == _promotionBonusModel.From.UtcDateTime &&
			x.To == _promotionBonusModel.To.UtcDateTime, cancellationToken);

		var playerBonuses = bonus.PromotionPlayers
			.Select(x => new DistributionQueueModel
			{
				PromotionId = x.PromotionBonus.PromotionId,
				PromotionBonusId = x.PromotionBonus.Id,
				PromotionPlayerId = x.Id,
				PlayerCode = x.PlayerCode,
				PromotionType = x.PromotionBonus.PromotionType,
				BucketAmount = x.BonusAmount * x.PromotionBonus.TurnoverMultiple,
				BonusAmount = x.BonusAmount,
				Description = "promotion turnover rebate weekly"
			})
			.ToArray();

		foreach (var playerBonus in playerBonuses)
		{
			await _taskQueue.QueuePlayerRebateAsync(playerBonus);
		}

		return this;
	}

	public async ValueTask<IDistribution> SetConditionDataAsync(CancellationToken cancellationToken)
	{
		var promotionLevels = await _tgsContext.PromotionVipLevels.Where(x => x.PromotionId == _promotion!.Id)
			.Select(v => v.Level).ToArrayAsync(cancellationToken: cancellationToken);

		var rule = _promotionSerializer.Deserialize<TurnoverRebateWeeklyRule>(_promotion!.CampaignDetail)!;

		var playerWinLossWeeklies = _tgsContext.PlayerGameRebateAggregates
			.Where(x => x.BrandId == _promotion.BrandId)
			.Where(x => x.ReportDateTime >= _previousMonday.UtcDateTime &&
				x.ReportDateTime <= _previousSunday.UtcDateTime)
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
				x.aggregate.VipLevel
			}, (player, list) => new PlayerTurnoverWeekly
			{
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel,
				GameProviderId = player.GameProviderId,
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
						.Distinct(), agg => agg.PlayerCode, playerCode => playerCode,
					(agg, playerCode) => new
					{
						agg.BrandId,
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
					Turnover = x.Turnover
				})
				.ToArrayAsync(cancellationToken).ConfigureAwait(false);
		}

		return this;
	}

	public IDistribution SetPromotion(PromotionRow promotion, DateTimeOffset from, DateTimeOffset to)
	{
		_promotion = promotion;
		_previousMonday = from;
		_previousSunday = to;

		return _promotion == null ? throw new ArgumentNullException(nameof(promotion)) : (IDistribution)this;
	}

	private static bool CheckIsAutoJoin(string campaignDetail)
	{
		return JsonSerializer
			.Deserialize<PlayerAutoJoinModel>(campaignDetail,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.PlayerAutoJoin ?? false;
	}
}
