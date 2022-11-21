namespace Tgs.Promotion.ApplicationServices;

public record PromotionIdsQuery(
	Guid BrandId,
	string PromotionName,
	string? PromotionType);
