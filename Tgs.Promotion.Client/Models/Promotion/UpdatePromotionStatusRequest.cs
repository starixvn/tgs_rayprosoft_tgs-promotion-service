namespace Tgs.Promotion.Client.Models.Promotion;

public record UpdatePromotionStatusRequest(Guid PromotionId, short Status);
