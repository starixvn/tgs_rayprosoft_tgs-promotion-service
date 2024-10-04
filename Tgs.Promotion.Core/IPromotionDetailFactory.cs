namespace Tgs.Promotion;

public interface IPromotionDetailFactory
{
	IPromotionDetailMessage GetPromotionDetail(string promotionType);

	IPromotionDetailMessage GetPromotionDetail(string promotionType, Func<bool> allowTypes);

	IPromotionDetailMessage GetDispatchesPromotionDetail(string promotionType);
}
