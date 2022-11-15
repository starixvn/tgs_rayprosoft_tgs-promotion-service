namespace Tgs.Promotion.Service.ViewModels;

public class DistributePromotionBonusViewModel
{
	public decimal Amount { get; init; }

	public string? Distributor { get; init; }

	public decimal NeedTurnover { get; init; }

	public Guid? ReferenceId { get; init; }
}
