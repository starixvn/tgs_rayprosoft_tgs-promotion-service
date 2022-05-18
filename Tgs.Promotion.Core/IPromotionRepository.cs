using System.Linq.Expressions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;

namespace Tgs.Promotion;

public interface IPromotionRepository
{
	ValueTask ChangePromotionStatusAsync(Domain.Entities.PromotionRow promotion);

	ValueTask<Guid> CreatePromotionAsync(CreatePromotionRequest request, PromotionType promotionTypeModel);

	ValueTask<Guid> CreatePromotionAsync(CreatePromotionInfo createInfo, CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(Guid promotionId, IEnumerable<int> vipLevels, string modifier, CancellationToken cancellationToken = default);

	ValueTask<bool> ExistsAsync(Expression<Func<Domain.Entities.PromotionRow, bool>> predicate);

	ValueTask UpdatePromotionAsync(Domain.Entities.PromotionRow promotion, UpdatePromotionRequest request);

	ValueTask<bool> UpdatePromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask UpdatePromotionsAsync(Domain.Entities.PromotionRow[] promotions);

	ValueTask<Domain.Entities.PromotionRow?> GetPromotionByIdAsync(Guid promotionId);

	ValueTask<Domain.Entities.PromotionRow?> GetPromotionAsync(
		Expression<Func<Domain.Entities.PromotionRow, bool>> predicate,
		Func<IQueryable<Domain.Entities.PromotionRow>, IQueryable<Domain.Entities.PromotionRow>>
			? preQuery = null);

	IQueryable<Domain.Entities.PromotionRow> GetPromotionQueries(
		Func<IQueryable<Domain.Entities.PromotionRow>,
			IQueryable<Domain.Entities.PromotionRow>>? preQuery = null,
		bool tracking = false);
}
