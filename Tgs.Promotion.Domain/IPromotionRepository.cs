using System.Linq.Expressions;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.Promotion;

namespace Tgs.Promotion.Domain
{
	public interface IPromotionRepository
	{
		ValueTask ChangePromotionStatusAsync(Entities.Promotion promotion);

		ValueTask<Guid> CreatePromotionAsync(CreatePromotionRequest request, PromotionType promotionTypeModel);

		ValueTask<bool> ExistsAsync(Expression<Func<Entities.Promotion, bool>> predicate);

		ValueTask UpdatePromotionAsync(Entities.Promotion promotion, UpdatePromotionRequest request);

		ValueTask<bool> UpdatePromotionAmountAsync(Guid promotionId, decimal amount);

		ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount);

		ValueTask UpdatePromotionsAsync(Entities.Promotion[] promotions);

		ValueTask<Entities.Promotion?> GetPromotionByIdAsync(Guid promotionId);

		ValueTask<Entities.Promotion?> GetPromotionAsync(
			Expression<Func<Entities.Promotion, bool>> predicate,
			Func<IQueryable<Entities.Promotion>, IQueryable<Entities.Promotion>>
				? preQuery = null);

		IQueryable<Entities.Promotion> GetPromotionQueries(
			Func<IQueryable<Entities.Promotion>,
				IQueryable<Entities.Promotion>>? preQuery = null,
			bool tracking = false);
	}
}
