namespace Tgs.Promotion.ApplicationServices;

public record CreateDepositPromotionParticipantQuery
	(Guid PromotionId, string PlayerCode);
