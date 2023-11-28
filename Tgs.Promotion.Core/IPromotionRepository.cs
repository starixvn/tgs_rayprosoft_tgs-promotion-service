using System.Text.Json;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion;

public interface IPromotionRepository
{
	ValueTask CreatePromotionDistributeAsync(
		PromotionDistributeInfo createInfo,
		string? distributor,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionDistributePlanInfo> CreatePromotionDistributePlanAsync(
		Guid promotionId,
		long conditionHash,
		CancellationToken cancellationToken = default);

	ValueTask CreatePromotionEventAsync(
		Guid eventId,
		Guid promotionId,
		PromotionEventAction action,
		JsonDocument changeSets,
		string? modifier,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionDistributePlanInfo?> FindPromotionDistributePlanAsync(
		Guid planId,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionSnapshot?> GetCurrentPromotionSnapshotAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	ValueTask<EventInfo?> GetEventDetailAsync(Guid promotionId,
		Guid eventId,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionRow?> GetPromotionByIdAsync(Guid promotionId);

	ValueTask<PromotionContentDetail?> GetPromotionContentAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	IQueryable<PromotionRow> GetPromotionQueries(
		Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>? preQuery = null,
		bool tracking = false);

	ValueTask<PromotionRule?> GetPromotionRuleAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	ValueTask<decimal> GetPromotionUsedAmountAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	ValueTask InsertPromotionDistributeLogAsync(
		PromotionDistributeLog log,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionView> ListPromotionsAsync(
		Guid brandId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<EventInfo> QueryPromotionEventsAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionView> ListPromotionsByFiltersAsync(
		Guid brandId,
		During? during,
		string? name,
		IEnumerable<string> promotionTypes,
		PromotionStatus? status,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionMainInfo> QueryPromotionsAsync(
		Guid brandId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionMainInfo> QueryPromotionsByPromotionTypeAsync(
		Guid brandId,
		string promotionType,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionMainInfo> QueryPromotionsByPromotionTypeAsync(
		string promotionType,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionGameProvidersAsync(
		Guid promotionId,
		IEnumerable<Guid> gameProviderIds,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(Guid promotionId,
		IEnumerable<int> vipLevels, string modifier,
		CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionDistributeAsync(
		Guid distributeId,
		DateTime sentOn,
		CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionSnapshotAsync(
		PromotionSnapshot snapshot,
		Guid snapshotId,
		DateTime updatedOn,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionDistributePlanDetailInfo> FindPromotionDistributePlanDetailAsync(
		Guid planId,
		CancellationToken cancellationToken = default);
}
