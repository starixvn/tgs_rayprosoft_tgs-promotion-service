using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CheckCanJoinPromotionQueryService(
	ISystemClock systemClock,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionRepository promotionRepository,
	IPromotionCheckerFactory promotionCheckerFactory) : IGetService<CheckCanJoinPromotionQuery, bool>
{
	private readonly IPromotionCheckerFactory _promotionCheckerFactory = promotionCheckerFactory
		?? throw new ArgumentNullException(nameof(promotionCheckerFactory));

	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	private readonly ISerialNumberGenerator _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));

	public async ValueTask<bool> GetAsync(
		CheckCanJoinPromotionQuery query,
		CancellationToken cancellationToken = default)
	{
		var snapshot = await _promotionRepository
			.GetCurrentPromotionSnapshotAsync(query.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null || snapshot.BrandId != query.BrandId)
			return false;

		var checker = _promotionCheckerFactory.GetPromotionChecker(snapshot.PromotionType);

		var isAllowed = await checker
			.CheckJoinAsync(
				new ConditionData(
					query.BrandId,
					query.PromotionId,
					query.PlayerCode,
					query.Amount),
				new PromotionMainInfo(
					snapshot.Id,
					snapshot.BrandId,
					snapshot.Name,
					(snapshot.From, snapshot.To),
					snapshot.PromotionType,
					snapshot.TotalAmountLimit,
					snapshot.AutoSentPlayerBonus),
				new PromotionRule(
					snapshot.CampaignDetail,
					snapshot.BonusAmountLimit,
					snapshot.Percentage,
					snapshot.TurnoverMultiple,
					snapshot.GameType,
					Array.Empty<int>()))
			.ConfigureAwait(false);

		if (!isAllowed)
			return false;

		if (snapshot.Unlimited == 0 && snapshot.AmountUsed >= snapshot.TotalAmountLimit)
			return false;

		var amount = snapshot.Percentage * query.Amount / 100M;
		amount = amount > snapshot.BonusAmountLimit ? snapshot.BonusAmountLimit : amount;

		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;

		var updateData = snapshot with
		{
			AmountUsed = snapshot.AmountUsed + amount,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);

		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.UpdateTotalUsedAmount,
				JsonSerializer.SerializeToDocument(changeSets),
				null,
				cancellationToken)
			.ConfigureAwait(false);

		await _promotionRepository.UpdatePromotionSnapshotAsync(
				updateData,
				eventId,
				currentTime,
				cancellationToken)
			.ConfigureAwait(false);

		return true;
	}
}
