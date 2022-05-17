namespace Tgs.Promotion.ApplicationServices;

public record CheckCanJoinPromotionQuery(
	Guid BrandId,
	string PlayerCode,
	Guid PromotionId,
	decimal Amount);
