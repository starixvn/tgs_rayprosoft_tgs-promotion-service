namespace Tgs.Promotion.Domain.Models.Promotion
{
	public record ChangePromotionStatusRequest(Guid Id, PromotionStatus Status);
}
