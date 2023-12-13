using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionMainInfosByPromotionTypeQueryService(IPromotionRepository promotionRepository) : IQueryService<GetPromotionMainInfosByPromotionTypeQuery, PromotionMainInfo>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public IAsyncEnumerable<PromotionMainInfo> QueryAsync(GetPromotionMainInfosByPromotionTypeQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.QueryPromotionsByPromotionTypeAsync(query.PromotionType, cancellationToken);
}
