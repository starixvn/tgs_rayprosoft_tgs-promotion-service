namespace Tgs.Promotion.ApplicationServices;

public record DistributePromotionBonusManuallyCommand(
	Guid PromotionDistributionId,
	string PlayerCode,
	string? Distributor);
