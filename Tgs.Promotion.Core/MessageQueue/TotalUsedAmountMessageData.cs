namespace Tgs.Promotion.MessageQueue;

public class TotalUsedAmountMessageData
{
	public Guid PromotionId { get; set; }

	public Guid SnapshotId { get; set; }

	public decimal TotalAmount { get; set; }
}
