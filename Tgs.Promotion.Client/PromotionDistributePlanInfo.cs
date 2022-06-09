namespace Tgs.Promotion.Clients;
public record PromotionDistributePlanInfo(
	Guid Id,
	Guid BrandId,
	Guid PromotionId,
	long ConditionHash);
