namespace Tgs.Promotion;

public interface IPromotionDistributePlanRepository
{
	ValueTask<Guid> CreatePromotionDistributePlanAsync(Guid brandId, Guid promotionId, int conditionHash, string detail);

	ValueTask<bool> ExistsAsync(Guid brandId, Guid promotionId, int conditionHash);

	ValueTask<Guid?> GetPromotionDistributePlanIdAsync(Guid brandId, Guid promotionId, int conditionHash);

}