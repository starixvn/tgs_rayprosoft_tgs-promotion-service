namespace Tgs.Promotion;

public record ConditionData(
	Guid BrandId,
	Guid PromotionId,
	string PlayerCode,
	decimal Amount);
