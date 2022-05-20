using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;

namespace Tgs.Promotion;

public interface IPromotionRepository
{
	ValueTask ChangePromotionStatusAsync(PromotionRow promotion);

	ValueTask<Guid> CreatePromotionAsync(CreatePromotionRequest request, PromotionType promotionTypeModel);

	ValueTask<Guid> CreatePromotionAsync(CreatePromotionMainInfo createInfo, string modifier, CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionMainInfoAsync(Guid promotionId, UpdatePromotionMainInfo updateInfo, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(Guid promotionId, IEnumerable<int> vipLevels, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionContentAsync(Guid promotionId, PromotionContentDetail content, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionStatusAsync(Guid promotionId, PromotionStatus status, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionRuleAsync(Guid promotionId, PromotionRule rule, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionGameProvidersAsync(Guid promotionId, IEnumerable<Guid> gameProviderIds, CancellationToken cancellationToken = default);

	ValueTask<bool> ExistsAsync(Expression<Func<PromotionRow, bool>> predicate);

	ValueTask UpdatePromotionAsync(PromotionRow promotion, UpdatePromotionRequest request);

	ValueTask<bool> UpdatePromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask UpdatePromotionsAsync(PromotionRow[] promotions);

	ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask<PromotionRow?> GetPromotionByIdAsync(Guid promotionId);

	ValueTask<PromotionRow?> GetPromotionAsync(
		Expression<Func<PromotionRow, bool>> predicate,
		Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>
			? preQuery = null);

	IQueryable<PromotionRow> GetPromotionQueries(
		Func<IQueryable<PromotionRow>,
			IQueryable<PromotionRow>>? preQuery = null,
		bool tracking = false);
}
