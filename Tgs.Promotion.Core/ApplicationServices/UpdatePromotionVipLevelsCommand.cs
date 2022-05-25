namespace Tgs.Promotion.ApplicationServices;
public record UpdatePromotionVipLevelsCommand(Guid PromotionId, IEnumerable<int> VipLevels, string Modifier);
