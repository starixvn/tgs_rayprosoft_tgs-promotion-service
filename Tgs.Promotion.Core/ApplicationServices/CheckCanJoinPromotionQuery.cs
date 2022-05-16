namespace Tgs.Promotion.Core.ApplicationServices
{
	public record CheckCanJoinPromotionQuery(
		Guid BrandId,
		string PlayerCode,
		Guid PromotionId,
		decimal Amount);
}
