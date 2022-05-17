namespace Tgs.Promotion.Models.Promotion;

public record ChangePromotionStatusRequest(Guid Id, PromotionStatus Status);
