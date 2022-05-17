namespace Tgs.Promotion.ApplicationServices;

public record RevertPromotionAmountCommand(Guid PromotionId, decimal Amount);
