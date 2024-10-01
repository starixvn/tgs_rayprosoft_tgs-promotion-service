using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Models.PromotionDistribute;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionDistributeQueryService(IPromotionDistributionRepository promotionDistributionRepository) : IGetService<GetPromotionDistributeQuery, PromotionDistributeModel?>
{
	public async ValueTask<PromotionDistributeModel?> GetAsync(GetPromotionDistributeQuery query,
		CancellationToken cancellationToken = default)
	{
		return await promotionDistributionRepository.GetPromotionDistributionQueries()
			.Include(x => x.Promotion)
			.Where(x => x.ReferenceId == query.ReferenceId)
			.Select(x => new PromotionDistributeModel
			{
				Id = x.Id,
				PromotionId = x.PromotionId,
				PromotionName = x.Promotion.Name,
				PromotionType = x.Promotion.PromotionType,
				CreatedOn = x.CreatedOn,
				SentOn = x.SentOn,
				PlayerCode = x.PlayerCode,
				BonusAmount = x.BonusAmount,
				ReferenceId = x.ReferenceId!.Value
			}).FirstOrDefaultAsync(cancellationToken);
	}
}
