namespace Tgs.Promotion.Core.ApplicationServices;

public record GetValidPromotionsByPlayerQuery(
	Guid BrandId,
	string PlayerCode);
