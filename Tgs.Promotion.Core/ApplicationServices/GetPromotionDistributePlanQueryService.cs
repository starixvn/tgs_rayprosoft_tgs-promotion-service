using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionDistributePlanQueryService : IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo?>
{
	private readonly IPromotionRepository _promotionRepository;
	private readonly IPromotionDistributePlanRepository _promotionDistributePlanRepository;

	public GetPromotionDistributePlanQueryService(IPromotionRepository promotionRepository, IPromotionDistributePlanRepository promotionDistributePlanRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionDistributePlanRepository = promotionDistributePlanRepository ?? throw new ArgumentNullException(nameof(promotionDistributePlanRepository));
	}

	public async ValueTask<PromotionDistributePlanInfo?> GetAsync(GetPromotionDistributePlanQuery query,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _promotionRepository.GetPromotionByIdAsync(query.PromotionId).ConfigureAwait(false);

		if (promotion is null)
		{
			return null;
		}

		return await _promotionDistributePlanRepository.GetPromotionDistributePlanInfoAsync(promotion.BrandId,
			promotion.Id, query.ConditionHash);
	}
}
