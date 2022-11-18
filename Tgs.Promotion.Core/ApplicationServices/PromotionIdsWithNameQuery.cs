namespace Tgs.Promotion.ApplicationServices;

public record PromotionIdsWithNameQuery(
	Guid BrandId,
	string PromotionName,
	string? PromotionType);
