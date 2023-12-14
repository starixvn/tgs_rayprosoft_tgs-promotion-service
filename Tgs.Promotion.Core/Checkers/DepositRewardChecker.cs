using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class DepositRewardChecker(
	string promotionType,
	PromotionSerializer promotionSerializer,
	IPromotionRepository promotionRepository) : IPromotionChecker
{
	public string PromotionType => promotionType;

	public async ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
	{
		if (extend is null
			|| !extend.RootElement.TryGetProperty("depositAmount", out var prop)
			|| !prop.TryGetDecimal(out var amount))
			return true;

		var snapshot = await promotionRepository
			.GetCurrentPromotionSnapshotAsync(promotion.Id, cancellationToken)
			.ConfigureAwait(false);
		if (snapshot is null)
			return false;

		var detail = promotionSerializer.Deserialize<DepositRewardSetting>(snapshot.CampaignDetail);

		return detail is not null && detail.DepositMinimumLimit <= amount;
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;
}
