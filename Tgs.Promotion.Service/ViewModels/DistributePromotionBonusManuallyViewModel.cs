namespace Tgs.Promotion.Service.ViewModels;

public class DistributePromotionBonusManuallyViewModel
{
	public string Distributor { get; set; } = default!;

	public DistributePlayerManuallyViewModel[] Players { get; set; } = default!;
}
