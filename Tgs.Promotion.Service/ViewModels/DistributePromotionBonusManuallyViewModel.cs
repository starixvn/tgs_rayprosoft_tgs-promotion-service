namespace Tgs.Promotion.Service.ViewModels;

public class DistributePromotionBonusManuallyViewModel
{
	public string? Distributor { get; init; }
	public IEnumerable<string>? BucketTypes { get; init; }
}
