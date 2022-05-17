using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion;

public interface IPromotionPlayerRepository
{
	ValueTask<PromotionPlayer?> GetPromotionPlayerAsync(
		Expression<Func<PromotionPlayer?, bool>> predicate,
		Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery = null);

	IQueryable<PromotionPlayer> GetPromotionPlayerQueries(
		Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery = null,
		bool tracking = false);
}
