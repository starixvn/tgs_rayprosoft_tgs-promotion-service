namespace Tgs.Promotion.Workers;

public class PromotionDistributionQueueModel
{
	public string? Distributor { get; init; }

	public string PlayerCode { get; set; } = default!;

	public Guid PromotionDistributionId { get; set; }
}
