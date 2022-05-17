namespace Tgs.Promotion;

public interface IPromotionJoinLockRepository
{
	ValueTask<bool> SetWhenNotExistsAsync(string playerCode, Guid promotionId);
}
