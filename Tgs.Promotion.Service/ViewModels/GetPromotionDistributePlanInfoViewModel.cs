namespace Tgs.Promotion.Service.ViewModels;

public record GetPromotionDistributePlanInfoViewModel
{
	public Guid PromotionId { get; init; }
	public int ConditionHash { get; init; }
}
