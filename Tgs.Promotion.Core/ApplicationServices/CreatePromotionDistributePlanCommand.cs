namespace Tgs.Promotion.Core.ApplicationServices;

public record CreatePromotionDistributePlanCommand(Guid PromotionId, decimal Amount);
