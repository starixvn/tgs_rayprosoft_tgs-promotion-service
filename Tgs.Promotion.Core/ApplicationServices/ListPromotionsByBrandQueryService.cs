using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class ListPromotionsByBrandQueryService : IQueryService<ListPromotionsByBrandQuery, PromotionView>
{
	private readonly IPromotionRepository _promotionRepository;

	public ListPromotionsByBrandQueryService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public IAsyncEnumerable<PromotionView> QueryAsync(ListPromotionsByBrandQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.ListPromotionsAsync(query.BrandId, cancellationToken);
}
