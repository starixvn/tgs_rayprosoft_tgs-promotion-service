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

	ValueTask CreatePromotionDistributeAsync(
		PromotionDistributeInfo createInfo,
		string? distributor,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionDistributePlanInfo> CreatePromotionDistributePlanAsync(
		Guid promotionId,
		long conditionHash,
		CancellationToken cancellationToken = default);

	ValueTask<bool> ExistsAsync(Expression<Func<PromotionRow, bool>> predicate);

	ValueTask<PromotionDistributePlanInfo?> FindPromotionDistributePlanAsync(Guid planId, CancellationToken cancellationToken = default);

	ValueTask<PromotionRow?> GetPromotionAsync(
		Expression<Func<PromotionRow, bool>> predicate,
		Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>? preQuery = null);

	ValueTask<PromotionRow?> GetPromotionByIdAsync(Guid promotionId);

	ValueTask<PromotionContentDetail?> GetPromotionContentAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(Guid promotionId, CancellationToken cancellationToken = default);

	IQueryable<PromotionRow> GetPromotionQueries(
		Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>? preQuery = null,
		bool tracking = false);

	ValueTask<PromotionRule?> GetPromotionRuleAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask InsertPromotionDistributeLogAsync(PromotionDistributeLog log, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionView> ListPromotionsAsync(Guid brandId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionMainInfo> QueryPromotionsAsync(
		Guid brandId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionMainInfo> QueryPromotionsByPromotionTypeAsync(
		Guid brandId,
		string promotionType,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionMainInfo> QueryPromotionsByPromotionTypeAsync(string promotionType, CancellationToken cancellationToken = default);

	ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask SetPromotionContentAsync(Guid promotionId, PromotionContentDetail content, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionGameProvidersAsync(Guid promotionId, IEnumerable<Guid> gameProviderIds, CancellationToken cancellationToken = default);

	ValueTask SetPromotionRuleAsync(Guid promotionId, PromotionRule rule, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionStatusAsync(Guid promotionId, PromotionStatus status, string modifier, CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(Guid promotionId, IEnumerable<int> vipLevels, string modifier, CancellationToken cancellationToken = default);

	ValueTask<bool> UpdatePromotionAmountAsync(Guid promotionId, decimal amount);

	ValueTask UpdatePromotionAsync(PromotionRow promotion, UpdatePromotionRequest request);

	ValueTask UpdatePromotionDistributeAsync(
		Guid distributeId,
		DateTime sentOn,
		CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionMainInfoAsync(Guid promotionId, UpdatePromotionMainInfo updateInfo, string modifier, CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionsAsync(PromotionRow[] promotions);

	ValueTask UpdatePromotionUsedAmountAsync(
		Guid promotionId,
		decimal totalAmount,
		Guid? snapshotId,
		CancellationToken cancellationToken = default);
}
