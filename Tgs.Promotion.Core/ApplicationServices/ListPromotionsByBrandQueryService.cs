using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class ListPromotionsByBrandQueryService(IPromotionRepository promotionRepository) : IQueryService<ListPromotionsByBrandQuery, PromotionView>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public IAsyncEnumerable<PromotionView> QueryAsync(ListPromotionsByBrandQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.ListPromotionsAsync(query.BrandId, cancellationToken)
			.Where(promotion => promotion.Status == PromotionStatus.Enabled);
}
