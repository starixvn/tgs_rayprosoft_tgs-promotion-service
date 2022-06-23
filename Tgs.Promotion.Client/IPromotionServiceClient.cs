namespace Tgs.Promotion.Clients;

public interface IPromotionServiceClient
{
	ValueTask<Guid> CreatePromotionAsync(
		Guid brandId,
		CreatePromotionMainInfo createInfo,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask<Guid> DistributePromotionBonusAsync(
		Guid planId,
		string playerCode,
		decimal amount,
		decimal needTurnover,
		string? distributor,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionDistributePlanInfo> GetPromotionDistributePlanAsync(
		Guid promotionId,
		long conditionHash,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask<TRule?> GetPromotionRuleAsync<TRule>(
		Guid promotionId,
		CancellationToken cancellationToken = default)
		where TRule : class;

	IAsyncEnumerable<PromotionView> ListPromotionsAsync(Guid brandId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionView> ListPromotionsAsync(Guid brandId, string playerCode, CancellationToken cancellationToken = default);

	ValueTask SetPromotionContentAsync(
		Guid promotionId,
		PromotionContent content,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionRuleAsync<TRule>(
		Guid promotionId,
		TRule rule,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionStatusAsync(
		Guid promotionId,
		PromotionStatus status,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(
		Guid promotionId,
		IEnumerable<int> vipLevels,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionMainInfoAsync(
		Guid promotionId,
		CreatePromotionMainInfo updateInfo,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask UpdatePromotionUsedAmountAsync(
		Guid promotionId,
		decimal totalAmount,
		Guid snapshotId,
		CancellationToken cancellationToken = default);
}
