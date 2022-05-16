namespace Tgs.Promotion.Core.Models.Promotion
{
	public record ChangePromotionStatusRequest(Guid Id, PromotionStatus Status);
}
