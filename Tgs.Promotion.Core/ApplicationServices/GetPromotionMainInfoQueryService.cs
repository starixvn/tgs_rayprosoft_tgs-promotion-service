using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionMainInfoQueryService : IGetService<GetPromotionMainInfoQuery, PromotionMainInfo?>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetPromotionMainInfoQueryService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<PromotionMainInfo?> GetAsync(GetPromotionMainInfoQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.GetPromotionMainInfoAsync(query.PromotionId, cancellationToken);
}
