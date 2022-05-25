namespace Tgs.Promotion.Models.Promotion;

public record CreateDepositPromotionParticipantRequest
	(Guid PromotionId, string PlayerCode);
