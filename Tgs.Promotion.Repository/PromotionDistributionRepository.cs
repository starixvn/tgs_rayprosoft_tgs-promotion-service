using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionDistributionRepository : IPromotionDistributionRepository
{
	private readonly ITgsService<PromotionDistribute> _promotionDistributeService;

	public PromotionDistributionRepository(
		ITgsService<PromotionDistribute> promotionDistributeService)
	{
		_promotionDistributeService = promotionDistributeService;
	}

	public IQueryable<PromotionDistribute> GetPromotionDistributionQueries(
		Func<IQueryable<PromotionDistribute>, IQueryable<PromotionDistribute>>? preQuery = null, bool tracking = false)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_promotionDistributeService.GetQueries())
			: preQuery(_promotionDistributeService.GetQueries()).AsNoTracking();
	}
}
