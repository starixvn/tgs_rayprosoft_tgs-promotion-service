namespace Tgs.Promotion.Core
{
	public interface IPromotionChecker
	{
		ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId = null);

		ValueTask CommitTagAsync(string playerCode, Guid? promotionId);

		ValueTask RollbackTagAsync(string playerCode, Guid? promotionId);

		ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId);
	}
}
