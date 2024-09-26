namespace Tgs.Promotion;

public interface IPromotionDetailFactory
{
	IPromotionDetailMessage GetPromotionDetail(string promotionType);
}
