#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.ApplicationServices.Promotion.Workers;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices.Promotion;

internal class ReliefMoneyWeeklyDistribution : IDistribution
{
	private readonly IPromotionBonusRepository _promotionBonusFactory;
	private readonly ICommandService<UpdatePromotionUsedAmountCommand> _promotionUsedAmountCommand;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IDistributionTaskQueue _taskQueue;
	private readonly TgsContext _tgsContext;
	private IEnumerable<PlayerWinLossWeekly> _playerWinLossWeekly = Array.Empty<PlayerWinLossWeekly>();

	private DateTimeOffset _previousMonday;

	private DateTimeOffset _previousSunday;

	private PromotionRow? _promotion;
	private PromotionBonusModel? _promotionBonusModel;

	public ReliefMoneyWeeklyDistribution(
		IDistributionTaskQueue taskQueue,
		IPromotionBonusRepository promotionBonusFactory,
		ISerialNumberGenerator serialNumberGenerator,
		ICommandService<UpdatePromotionUsedAmountCommand> promotionUsedAmountCommand,
		TgsContext tgsContext)
	{
		_taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
		_promotionBonusFactory =
			promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
		_promotionUsedAmountCommand = promotionUsedAmountCommand ?? throw new ArgumentNullException(nameof(promotionUsedAmountCommand));
		_serialNumberGenerator =
			serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
	}

	public async ValueTask<IDistribution> CalculateBonusAmountAsync(CancellationToken cancellationToken)
	{
		var promotionLevels = await _tgsContext.PromotionVipLevels.Where(x => x.PromotionId == _promotion!.Id)
			.Select(v => v.Level).ToArrayAsync(cancellationToken: cancellationToken);

		var promotionGameProviderIds = await _tgsContext.PromotionGameProviders
			.Where(x => x.PromotionId == _promotion!.Id)
			.Select(v => v.GameProviderId).ToArrayAsync(cancellationToken: cancellationToken);

		var id = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);

		var amount = _promotion!.AmountUsed;

		var playerBonus = _playerWinLossWeekly.GroupBy(x => x.PlayerCode,
			(playerCode, list) => new
			{
				PlayerCode = playerCode,
				WinLoss = list.Sum(s => s.WinLoss)
			}).Select(x =>
		{
			if (amount > _promotion.TotalAmountLimit && _promotion.Unlimited == 0)
			{
				return new PromotionBonusPlayerModel
				{
					PromotionBonusId = id,
					PlayerCode = x.PlayerCode,
					BonusAmount = 0M,
					PromotionType = _promotion.PromotionType
				};
			}

			var playerRebateAmount = x.WinLoss * _promotion.Percentage / 100;

			if (playerRebateAmount <= 0)
			{
				return new PromotionBonusPlayerModel
				{
					PromotionBonusId = id,
					PlayerCode = x.PlayerCode,
					BonusAmount = 0,
					PromotionType = _promotion.PromotionType
				};
			}

			playerRebateAmount = playerRebateAmount > _promotion.BonusAmountLimit
				? _promotion.BonusAmountLimit
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
			GameProviderIds = promotionGameProviderIds,
			VipLevels = promotionLevels,
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
			From = _previousMonday,
			To = _previousSunday,
			AutoSentPlayerBonus = _promotion.AutoSentPlayerBonus,
			PromotionType = _promotion.PromotionType,
			TotalAmountLimit = _promotion.TotalAmountLimit,
			GameType = (GameType?)_promotion.GameType,
			Percentage = _promotion.Percentage,
			BonusAmountLimit = _promotion.BonusAmountLimit,
			TurnoverMultiple = _promotion.TurnoverMultiple,
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
			await _promotionUsedAmountCommand.ExecuteAsync(
				new UpdatePromotionUsedAmountCommand(_promotion!.Id, _promotion.AmountUsed, null), cancellationToken);

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
				Description = "promotion relief money weekly"
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

		var promotionGameProviderIds = await _tgsContext.PromotionGameProviders
			.Where(x => x.PromotionId == _promotion!.Id)
			.Select(v => v.GameProviderId).ToArrayAsync(cancellationToken: cancellationToken);

		var playerWinLossWeeklies = _tgsContext.PlayerGameRebateAggregates
			.Where(x => x.BrandId == _promotion!.BrandId)
			.Where(x => x.ReportDateTime >= _previousMonday.UtcDateTime &&
				x.ReportDateTime < _previousSunday.AddDays(1).UtcDateTime)
			.Where(promotionLevels.Length > 0,
				pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
			.Where(_promotion!.GameType is not null, pa => pa.GameType == _promotion.GameType)
			.Where(promotionGameProviderIds.Any(), pa =>
				promotionGameProviderIds.Contains(pa.GameProviderId))
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
			}, (player, list) => new PlayerWinLossWeekly
			{
				BrandId = player.BrandId,
				PlayerCode = player.PlayerCode,
				GameType = player.GameType,
				VipLevel = player.VipLevel,
				GameProviderId = player.GameProviderId,
				WinLoss = list.Sum(l => l.aggregate.WinLoss)
			});

		_playerWinLossWeekly = CheckIsAutoJoin(_promotion.CampaignDetail)
			? await playerWinLossWeeklies.ToArrayAsync(cancellationToken).ConfigureAwait(false)
			: (IEnumerable<PlayerWinLossWeekly>)await playerWinLossWeeklies
				.Join(
					_tgsContext.PromotionParticipants
						.Where(pp => pp.PromotionId == _promotion.Id)
						.Where(x => x.CreatedOn >= _promotion.From && x.CreatedOn <= _promotion.To)
						.Select(x => x.PlayerCode)
						.Distinct(), agg => agg.PlayerCode, playerCode => playerCode,
					(agg, playerCode) => new
					{
						agg.BrandId,
						agg.PlayerCode,
						agg.GameType,
						agg.GameProviderId,
						agg.VipLevel,
						agg.WinLoss
					})
				.Select(x => new PlayerWinLossWeekly
				{
					BrandId = x.BrandId,
					PlayerCode = x.PlayerCode,
					GameType = x.GameType,
					VipLevel = x.VipLevel,
					GameProviderId = x.GameProviderId,
					WinLoss = x.WinLoss
				})
				.ToArrayAsync(cancellationToken).ConfigureAwait(false);

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
