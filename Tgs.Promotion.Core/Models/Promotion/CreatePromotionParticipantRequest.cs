namespace Tgs.Promotion.Core.Models.Promotion;

public record CreatePromotionParticipantRequest(Guid PromotionId, string PlayerCode);
