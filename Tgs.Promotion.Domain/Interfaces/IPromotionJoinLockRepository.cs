namespace Tgs.Promotion.Domain.Interfaces
{
    public interface IPromotionJoinLockRepository
    {
        ValueTask<bool> SetWhenNotExistsAsync(string playerCode, Guid promotionId);
    }
}
