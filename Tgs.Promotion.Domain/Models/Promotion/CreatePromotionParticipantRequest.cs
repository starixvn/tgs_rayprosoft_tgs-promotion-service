namespace Tgs.Promotion.Domain.Models.Promotion
{
	public record CreatePromotionParticipantRequest(Guid PromotionId, string PlayerCode);
}
