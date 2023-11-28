namespace Tgs.Promotion.Workers;

public class PromotionDistributionQueueModel
{
	public Guid PromotionDistributionId { get; set; }
	public string PlayerCode { get; set; } = default!;
	public string? Distributor { get; init; }
}