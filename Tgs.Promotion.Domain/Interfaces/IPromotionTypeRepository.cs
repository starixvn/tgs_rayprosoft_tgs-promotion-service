namespace Tgs.Promotion.Domain.Interfaces
{
    public interface IPromotionTypeRepository
    {
        ValueTask<bool> IsDepositTypeAsync(string promotionType);
    }
}