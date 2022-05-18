namespace Tgs.Promotion;

public interface IPromotionTypeRepository
{
	ValueTask<bool> IsDepositTypeAsync(string promotionType);
}
