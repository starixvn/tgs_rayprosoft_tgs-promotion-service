namespace Tgs.Promotion.Clients.Models.Promotion;

public record UpdatePromotionStatusRequest(Guid PromotionId, short Status);
