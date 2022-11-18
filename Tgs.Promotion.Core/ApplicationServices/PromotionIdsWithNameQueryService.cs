using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class PromotionIdsWithNameQueryService : IQueryService<PromotionIdsWithNameQuery, Guid>
{
	private readonly IPromotionRepository _promotionRepository;

	public PromotionIdsWithNameQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public IAsyncEnumerable<Guid> QueryAsync(
		PromotionIdsWithNameQuery query,
		CancellationToken cancellationToken = default)
	{
		var results = _promotionRepository.QueryPromotionsWithNameAsync(
			query.BrandId,
			query.PromotionName,
			cancellationToken);

		if (!string.IsNullOrEmpty(query.PromotionType))
			results = results.Where(promotion => promotion.PromotionType == query.PromotionType);

		return results.Select(promotion => promotion.Id);
	}
}
