namespace Tgs.Promotion.Domain.Interfaces
{
    public interface IPromotionCheckerFactory
    {
        IPromotionChecker GetPromotionChecker(string promotionType);
    }
}