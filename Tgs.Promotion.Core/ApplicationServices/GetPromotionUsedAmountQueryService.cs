using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionUsedAmountQueryService(
	IPromotionRepository promotionRepository) : IGetService<GetPromotionUsedAmountQuery, decimal>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public ValueTask<decimal> GetAsync(
		GetPromotionUsedAmountQuery query, 
		CancellationToken cancellationToken = default)
	{
		return _promotionRepository.GetPromotionUsedAmountAsync(query.PromotionId, cancellationToken);
	}
}
