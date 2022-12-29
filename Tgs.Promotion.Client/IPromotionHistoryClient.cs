namespace Tgs.Promotion.Clients;

public interface IPromotionHistoryClient
{
	ValueTask<PromotionEventDetailInfo?> GetPromotionEventDetailAsync(
		Guid promotionId,
		Guid eventId, 
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionEventInfo> QueryPromotionEventsAsync(
		Guid promotionId, 
		CancellationToken cancellationToken = default);
}