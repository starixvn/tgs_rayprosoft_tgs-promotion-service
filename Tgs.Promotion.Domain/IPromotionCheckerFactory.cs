namespace Tgs.Promotion.Domain
{
	public interface IPromotionCheckerFactory
	{
		IPromotionChecker GetPromotionChecker(string promotionType);
	}
}
