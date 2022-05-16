using System.Linq.Expressions;
using Tgs.Promotion.Core.Models;
using Tgs.Promotion.Core.Models.Promotion;

namespace Tgs.Promotion.Core;

public interface IPromotionRepository
{
	ValueTask ChangePromotionStatusAsync(Domain.Entities.Promotion promotion);

	ValueTask<Guid> CreatePromotionAsync(CreatePromotionRequest request, PromotionType promotionTypeModel);

	ValueTask<bool> ExistsAsync(Expression<Func<Domain.Entities.Promotion, bool>> predicate);

	ValueTask UpdatePromotionAsync(Domain.Entities.Promotion promotion, UpdatePromotionRequest request);

	ValueTask<bool> UpdatePromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask UpdatePromotionsAsync(Domain.Entities.Promotion[] promotions);

	ValueTask<Domain.Entities.Promotion?> GetPromotionByIdAsync(Guid promotionId);

	ValueTask<Domain.Entities.Promotion?> GetPromotionAsync(
		Expression<Func<Domain.Entities.Promotion, bool>> predicate,
		Func<IQueryable<Domain.Entities.Promotion>, IQueryable<Domain.Entities.Promotion>>
			? preQuery = null);

	IQueryable<Domain.Entities.Promotion> GetPromotionQueries(
		Func<IQueryable<Domain.Entities.Promotion>,
			IQueryable<Domain.Entities.Promotion>>? preQuery = null,
		bool tracking = false);
}
