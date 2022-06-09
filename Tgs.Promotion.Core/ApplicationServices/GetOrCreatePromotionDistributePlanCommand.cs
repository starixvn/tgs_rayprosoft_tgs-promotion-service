namespace Tgs.Promotion.ApplicationServices;
public record GetOrCreatePromotionDistributePlanCommand(Guid PromotionId, long ConditionHash);
