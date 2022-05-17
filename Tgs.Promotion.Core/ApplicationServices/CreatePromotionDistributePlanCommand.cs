namespace Tgs.Promotion.ApplicationServices;

public record CreatePromotionDistributePlanCommand(Guid PromotionId, decimal Amount);
