namespace Tgs.Promotion.Domain
{
	public interface IPromotionTypeRepository
	{
		ValueTask<bool> IsDepositTypeAsync(string promotionType);
	}
}
