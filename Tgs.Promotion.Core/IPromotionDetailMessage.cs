namespace Tgs.Promotion;

public interface IPromotionDetailMessage
{
	ValueTask<DistributedDetailResponse?> GetDetailAsync(Guid promotionId, Guid referenceId, TimeSpan? timeout = null,
		CancellationToken cancellationToken = default);
}
