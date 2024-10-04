using System.Text.Json;
using Tgs.Promotion.Models.PromotionDistribute;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionDistributeDetailQueryService(IPromotionRepository promotionRepository, IPromotionDetailFactory promotionDetailFactory) : IGetService<GetPromotionDistributeDetailQuery, PromotionDistributeDetailModel?>
{
	public async ValueTask<PromotionDistributeDetailModel?> GetAsync(GetPromotionDistributeDetailQuery query,
		CancellationToken cancellationToken = default)
	{
		var promotion = await promotionRepository.GetPromotionMainInfoAsync(query.PromotionId, cancellationToken);

		if (promotion == null)
			return null;

		var response = await promotionDetailFactory.GetPromotionDetail(promotion.PromotionType)
			.GetDetailAsync(promotion.Id, query.ReferenceId, cancellationToken: cancellationToken);

		return new PromotionDistributeDetailModel
		{
			BrandId = promotion.BrandId,
			PromotionId = promotion.Id,
			ReferenceId = query.ReferenceId,
			Detail = response?.Detail == null ? null : JsonDocument.Parse(response.Detail)
		};
	}
}
