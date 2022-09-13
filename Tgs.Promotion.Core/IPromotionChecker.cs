namespace Tgs.Promotion;

public interface IPromotionChecker
{
	ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion);

	ValueTask CommitTagAsync(string playerCode, Guid? promotionId);

	ValueTask RollbackTagAsync(string playerCode, Guid? promotionId);

	ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId);

	ValueTask<bool> CheckJoinAsync(ConditionData conditionData, PromotionMainInfo promotion,PromotionRule rule);
}
