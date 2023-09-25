using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class ListPromotionsByFiltersQueryService : IQueryService<ListPromotionsByFilters, PromotionView>
{
	private readonly IPromotionRepository _promotionRepository;

	public ListPromotionsByFiltersQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public IAsyncEnumerable<PromotionView> QueryAsync(
		ListPromotionsByFilters query,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.ListPromotionsByFiltersAsync(
			query.BrandId,
			query.EventDuring,
			query.Name,
			query.PromotionTypes,
			query.Status,
			cancellationToken);
}
