using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionDistributionRepository(
	ITgsService<PromotionDistribute> promotionDistributeService) : IPromotionDistributionRepository
{
	public IQueryable<PromotionDistribute> GetPromotionDistributionQueries(
		Func<IQueryable<PromotionDistribute>, IQueryable<PromotionDistribute>>? preQuery = null, bool tracking = false)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(promotionDistributeService.GetQueries())
			: preQuery(promotionDistributeService.GetQueries()).AsNoTracking();
	}
}
