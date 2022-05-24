namespace Tgs.Promotion.Clients;

public interface IPromotionServiceClient
{
	ValueTask<Guid> CreatePromotionAsync(
		Guid brandId,
		CreatePromotionMainInfo createInfo,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask SetPromotionContentAsync(
		Guid promotionId,
		PromotionContent content,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask<TRule?> GetPromotionRuleAsync<TRule>(
		Guid promotionId,
		CancellationToken cancellationToken = default)
		where TRule : class;

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

	ValueTask<PromotionDistributePlanInfo?> GetPromotionDistributePlanAsync(
		Guid promotionId,
		long conditionHash,
		CancellationToken cancellationToken = default);

	ValueTask<Guid?> CreatePromotionDistributeAsync(
		Guid planId, 
		Guid promotionId, 
		int conditionHash, 
		string playerCode,
		decimal amount, 
		string? distributor,
		CancellationToken cancellationToken = default);
}
