namespace Tgs.Promotion.Service.ViewModels;

public record DistributePromotionBonusViewModel
{
	public Guid PlanId { get; init; }
	public Guid PromotionId { get; init; }
	public int ConditionHash { get; init; }
	public string PlayerCode { get; init; } = default!;
	public decimal Amount { get; init; }
	public string? Distributor { get; init; }
}