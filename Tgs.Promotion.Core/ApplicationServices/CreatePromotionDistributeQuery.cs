namespace Tgs.Promotion.ApplicationServices;

public record CreatePromotionDistributeQuery(Guid BrandId, Guid PromotionId, int ConditionHash,string PlayerCode, decimal Amount, string? Distributor);
