using System.Text.Json;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CheckCanJoinPromotionQueryService(
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

		if (snapshot.Unlimited == 0 && snapshot.AmountUsed >= snapshot.TotalAmountLimit && snapshot.TotalAmountLimit > 0M)
			return false;

		return true;
	}
}
