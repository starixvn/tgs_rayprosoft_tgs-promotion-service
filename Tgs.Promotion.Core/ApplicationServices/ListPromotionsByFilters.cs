namespace Tgs.Promotion.ApplicationServices;

public record ListPromotionsByFilters(
	Guid BrandId,
	During? EventDuring,
	string? Name,
	IEnumerable<string> PromotionTypes,
	PromotionStatus? Status);
