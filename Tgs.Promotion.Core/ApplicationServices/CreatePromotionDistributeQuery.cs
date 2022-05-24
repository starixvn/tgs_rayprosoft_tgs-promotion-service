namespace Tgs.Promotion.ApplicationServices;

public record CreatePromotionDistributeQuery(Guid PlanId, Guid PromotionId, int ConditionHash,string PlayerCode, decimal Amount, string? Distributor);
