namespace Tgs.Promotion.ApplicationServices;

public record CreatePromotionMainInfoCommand(
	Guid BrandId,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	bool IsAutoSent,
	string Modifier);
