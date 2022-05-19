namespace Tgs.Promotion;
public record PromotionMainInfo(
	Guid Id,
	Guid BrandId,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	bool IsAutoSent);
