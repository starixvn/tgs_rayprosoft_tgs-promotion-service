namespace Tgs.Promotion.ApplicationServices;

public record GetValidPromotionsByPlayerQuery(
	Guid BrandId,
	string PlayerCode);
