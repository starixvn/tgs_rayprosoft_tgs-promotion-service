namespace Tgs.Promotion.Models.Promotion;

public record CreatePromotionParticipantRequest(Guid PromotionId, string PlayerCode);
