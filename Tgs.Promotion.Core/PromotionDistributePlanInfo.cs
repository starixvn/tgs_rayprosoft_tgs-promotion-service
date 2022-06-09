namespace Tgs.Promotion;
public record PromotionDistributePlanInfo(
	Guid Id,
	Guid BrandId,
	Guid PromotionId,
	long ConditionHash);
