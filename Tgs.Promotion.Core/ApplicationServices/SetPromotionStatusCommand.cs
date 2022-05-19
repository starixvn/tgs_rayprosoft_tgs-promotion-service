namespace Tgs.Promotion.ApplicationServices;
public record SetPromotionStatusCommand(Guid PromotionId, PromotionStatus Status, string Modifier);
