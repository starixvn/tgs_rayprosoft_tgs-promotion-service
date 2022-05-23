namespace Tgs.Promotion.ApplicationServices;
public record UpdatePromotionMainInfoCommand(
	Guid PromotionId,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	bool IsAutoSent,
	string Modifier);
