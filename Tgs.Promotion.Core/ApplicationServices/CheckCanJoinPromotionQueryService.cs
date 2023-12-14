using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CheckCanJoinPromotionQueryService(
	ISystemClock systemClock,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionRepository promotionRepository,
	IPromotionCheckerFactory promotionCheckerFactory,
	PromotionSerializer promotionSerializer) : IGetService<CheckCanJoinPromotionQuery, bool>
{
	public async ValueTask<bool> GetAsync(
		CheckCanJoinPromotionQuery query,
		CancellationToken cancellationToken = default)
	{
		var snapshot = await promotionRepository
			.GetCurrentPromotionSnapshotAsync(query.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null || snapshot.BrandId != query.BrandId)
			return false;

		var checker = promotionCheckerFactory.GetPromotionChecker(snapshot.PromotionType);

		var isAllowed = await checker
			.CheckAsync(
				query.PlayerCode,
				new PromotionMainInfo(
					snapshot.Id,
					snapshot.BrandId,
					snapshot.Name,
					(snapshot.From, snapshot.To),
					snapshot.PromotionType,
					snapshot.TotalAmountLimit,
					snapshot.AutoSentPlayerBonus),
				JsonDocument.Parse(
					promotionSerializer.Serialize(new
					{
						DepositAmount = query.Amount
					})),
				cancellationToken)
			.ConfigureAwait(false);

		if (!isAllowed)
			return false;

		if (snapshot.Unlimited == 0 && snapshot.AmountUsed >= snapshot.TotalAmountLimit)
			return false;

		var amount = snapshot.Percentage * query.Amount / 100M;
		amount = amount > snapshot.BonusAmountLimit ? snapshot.BonusAmountLimit : amount;

		var eventId = await serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = systemClock.UtcNow.UtcDateTime;

		var updateData = snapshot with
		{
			AmountUsed = snapshot.AmountUsed + amount,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);

		await promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.UpdateTotalUsedAmount,
				JsonSerializer.SerializeToDocument(changeSets),
				null,
				cancellationToken)
			.ConfigureAwait(false);

		await promotionRepository.UpdatePromotionSnapshotAsync(
				updateData,
				eventId,
				currentTime,
				cancellationToken)
			.ConfigureAwait(false);

		return true;
	}
}
