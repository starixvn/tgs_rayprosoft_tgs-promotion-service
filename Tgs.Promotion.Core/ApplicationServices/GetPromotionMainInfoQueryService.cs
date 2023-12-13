using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionMainInfoQueryService(IPromotionRepository promotionRepository) : IGetService<GetPromotionMainInfoQuery, PromotionMainInfo?>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public ValueTask<PromotionMainInfo?> GetAsync(GetPromotionMainInfoQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.GetPromotionMainInfoAsync(query.PromotionId, cancellationToken);
}
