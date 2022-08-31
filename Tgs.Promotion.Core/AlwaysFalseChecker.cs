namespace Tgs.Promotion;

internal class AlwaysFalseChecker : IPromotionChecker
{
	public ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
		=> ValueTask.FromResult(false);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
	public ValueTask<bool> CheckJoinAsync(ConditionData conditionData, PromotionMainInfo promotion, PromotionRule rule) => throw new NotImplementedException();

	public ValueTask<bool> CheckJoinAsync<IConditionData1>(string playerCode, PromotionMainInfo promotion,
		IConditionData1 conditionData)
		=> throw new NotImplementedException();
}
