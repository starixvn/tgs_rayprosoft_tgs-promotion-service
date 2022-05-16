namespace Tgs.Promotion.Core.Services
{
	public record GetValidPromotionsByPlayerQuery(
		Guid BrandId,
		string PlayerCode);
}
