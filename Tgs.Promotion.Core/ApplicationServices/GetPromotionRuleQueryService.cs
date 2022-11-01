using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionRuleQueryService : IGetService<GetPromotionRuleQuery, PromotionRuleView?>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetPromotionRuleQueryService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<PromotionRuleView?> GetAsync(GetPromotionRuleQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.GetPromotionRuleAsync(query.PromotionId, cancellationToken);
}
