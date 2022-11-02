using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionContentQueryService : IGetService<GetPromotionContentQuery, PromotionContentInfo?>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetPromotionContentQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<PromotionContentInfo?> GetAsync(
		GetPromotionContentQuery query,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.GetPromotionContentAsync(query.PromotionId, cancellationToken);
}
