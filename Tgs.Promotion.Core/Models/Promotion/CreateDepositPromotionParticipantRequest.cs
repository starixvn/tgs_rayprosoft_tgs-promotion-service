namespace Tgs.Promotion.Core.Models.Promotion
{
	public record CreateDepositPromotionParticipantRequest
		(Guid PromotionId, string PlayerCode);
}
