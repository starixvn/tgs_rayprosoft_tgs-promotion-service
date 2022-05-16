namespace Tgs.Promotion.Core.ApplicationServices;

public record RevertPromotionAmountCommand(Guid PromotionId, decimal Amount);
