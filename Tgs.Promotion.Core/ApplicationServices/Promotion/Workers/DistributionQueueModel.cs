namespace Tgs.Promotion.ApplicationServices.Promotion.Workers;

public record DistributionQueueModel
{
	public Guid PromotionId { get; init; }
	public Guid PromotionBonusId { get; init; }
	public int PromotionPlayerId { get; init; }
	public string PlayerCode { get; init; } = default!;
	public string PromotionType { get; init; } = default!;
	public decimal BucketAmount { get; init; }
	public decimal BonusAmount { get; init; }
	public string Description { get; init; } = default!;
}
