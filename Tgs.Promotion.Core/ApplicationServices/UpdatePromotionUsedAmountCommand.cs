namespace Tgs.Promotion.ApplicationServices;
public record UpdatePromotionUsedAmountCommand(Guid PromotionId, decimal TotalAmount, Guid SnapshotId);
