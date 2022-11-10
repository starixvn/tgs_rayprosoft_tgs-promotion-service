namespace Tgs.Promotion;
public record PromotionDistributeInfo(
	Guid Id,
	Guid BrandId,
	Guid? PlanId,
	string PlayerCode,
	Guid PromotionId,
	decimal BonusAmount,
	decimal Bucket,
	DateTime? SentOn);
