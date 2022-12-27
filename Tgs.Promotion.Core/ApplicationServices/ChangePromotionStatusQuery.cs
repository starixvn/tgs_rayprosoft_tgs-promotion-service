namespace Tgs.Promotion.ApplicationServices;

public record ChangePromotionStatusQuery(Guid Id, PromotionStatus Status);
