namespace Tgs.Promotion.Clients;

public interface IPromotionServiceClient
{
	ValueTask<Guid> CreatePromotionAsync(
		Guid brandId,
		CreatePromotionMainInfo createInfo,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(
		Guid promotionId,
		IEnumerable<int> vipLevels,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionContentAsync(
		Guid promotionId,
		PromotionContent content,
		string modifier,
		CancellationToken cancellationToken = default);
}
