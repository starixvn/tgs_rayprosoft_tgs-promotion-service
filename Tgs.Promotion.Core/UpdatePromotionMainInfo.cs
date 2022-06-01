namespace Tgs.Promotion;

public record UpdatePromotionMainInfo(
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	bool IsAutoSent);