namespace Tgs.Promotion.ApplicationServices;

public record ListPromotionsByFilters(
	Guid BrandId,
	During? EventDuring,
	string? Name,
	string? PromotionType,
	PromotionStatus? Status);
