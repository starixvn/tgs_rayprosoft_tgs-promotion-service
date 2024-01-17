namespace Tgs.Promotion.Clients;

public interface IPromotionHistoryClient
{
	ValueTask<PromotionEventDetailInfo?> GetPromotionEventDetailAsync(
		Guid promotionId,
		Guid eventId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionEventMainInfo> QueryPromotionEventsAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);
}
