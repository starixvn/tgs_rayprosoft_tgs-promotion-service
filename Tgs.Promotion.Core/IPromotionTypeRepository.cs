namespace Tgs.Promotion.Core;

public interface IPromotionTypeRepository
{
	ValueTask<bool> IsDepositTypeAsync(string promotionType);
}
