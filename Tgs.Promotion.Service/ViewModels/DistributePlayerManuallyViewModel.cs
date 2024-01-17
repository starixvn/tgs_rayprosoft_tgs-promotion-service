namespace Tgs.Promotion.Service.ViewModels;

public class DistributePlayerManuallyViewModel
{
	public string PlayerCode { get; set; } = default!;

	public Guid PromotionBonusId { get; set; }
}
