namespace Tgs.Promotion;

public record CreatePromotionMainInfo(
	Guid BrandId,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	bool IsAutoSent);
