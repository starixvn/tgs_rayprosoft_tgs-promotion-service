using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionContentQueryService : IGetService<GetPromotionContentQuery, PromotionContentDetail?>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetPromotionContentQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<PromotionContentDetail?> GetAsync(
		GetPromotionContentQuery query,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.GetPromotionContentAsync(query.PromotionId, cancellationToken);
}
