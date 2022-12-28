namespace Tgs.Promotion.ApplicationServices;

public record ChangePromotionOrdersCommand(
	IEnumerable<PromotionOrder> Orders,
	string Modifier);
