namespace Tgs.Promotion.ApplicationServices;

public record DistributePromotionBonusCommand(
	Guid PlanId,
	string PlayerCode,
	decimal Amount,
	decimal NeedTurnover,
	Guid? ReferenceId,
	string? Distributor,
	string? BucketType = null);
