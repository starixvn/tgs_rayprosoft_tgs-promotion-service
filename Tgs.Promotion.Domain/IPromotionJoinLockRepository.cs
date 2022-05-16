namespace Tgs.Promotion.Domain
{
	public interface IPromotionJoinLockRepository
	{
		ValueTask<bool> SetWhenNotExistsAsync(string playerCode, Guid promotionId);
	}
}
