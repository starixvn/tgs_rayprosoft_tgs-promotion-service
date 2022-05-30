namespace Tgs.Promotion.ApplicationServices;

public record DistributePromotionBonusCommand(
	Guid PlanId,
	string PlayerCode,
	decimal Amount,
	decimal NeedTurnover,
	string? Distributor);
