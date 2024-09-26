namespace Tgs.Promotion.PromotionDetailMessages;

public class NullDetailMessageReply(string promotionType) : IPromotionDetailMessage
{
	public string PromotionType => promotionType;

	public ValueTask<DistributedDetailResponse?> GetDetailAsync(Guid promotionId, Guid referenceId,
		TimeSpan? timeout,
		CancellationToken cancellationToken = default)
	{
		return new ValueTask<DistributedDetailResponse?>();
	}
}
