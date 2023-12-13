namespace Tgs.Promotion.Checkers;

internal class DepositRewardChecker(PromotionSerializer promotionSerializer) : IPromotionChecker
{
	private readonly PromotionSerializer _promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));

	public ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
		=> ValueTask.FromResult(true);

	public ValueTask<bool> CheckJoinAsync(ConditionData conditionData, PromotionMainInfo promotion, PromotionRule rule)
	{
		var detail = _promotionSerializer.Deserialize<DepositRewardSetting>(rule.CampaignDetail);
		return ValueTask.FromResult(detail != null && detail.DepositMinimumLimit <= conditionData.Amount);
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
}
