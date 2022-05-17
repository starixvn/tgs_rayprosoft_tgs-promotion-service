namespace Tgs.Promotion;

public interface IPromotionCheckerFactory
{
	IPromotionChecker GetPromotionChecker(string promotionType);
}
