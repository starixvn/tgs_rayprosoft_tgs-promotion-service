namespace Tgs.Promotion.Core
{
	public interface IPromotionCheckerFactory
	{
		IPromotionChecker GetPromotionChecker(string promotionType);
	}
}
