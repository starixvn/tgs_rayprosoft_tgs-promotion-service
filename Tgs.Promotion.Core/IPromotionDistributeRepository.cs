namespace Tgs.Promotion;

public interface IPromotionDistributeRepository
{
	ValueTask<Guid> CreatePromotionDistributeAsync(Guid brandId, Guid promotionId, Guid? planId, decimal bonusAmount,
		string promotionType, string playerCode, string? distributor);
}