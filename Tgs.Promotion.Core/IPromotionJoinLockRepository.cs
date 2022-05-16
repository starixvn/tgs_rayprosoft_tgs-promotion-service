namespace Tgs.Promotion.Core
{
	public interface IPromotionJoinLockRepository
	{
		ValueTask<bool> SetWhenNotExistsAsync(string playerCode, Guid promotionId);
	}
}
