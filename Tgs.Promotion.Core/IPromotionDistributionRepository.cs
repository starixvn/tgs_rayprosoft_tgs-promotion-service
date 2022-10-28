using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion;

public interface IPromotionDistributionRepository
{
	IQueryable<PromotionDistribute> GetPromotionDistributionQueries(
			Func<IQueryable<PromotionDistribute>, IQueryable<PromotionDistribute>>? preQuery = null,
		bool tracking = false);
}
