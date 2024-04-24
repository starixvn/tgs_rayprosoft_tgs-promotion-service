namespace Tgs.Promotion.Service.ViewModels;

public class PromotionOrdersViewModel
{
	public string Modifier { get; init; } = default!;

	public IEnumerable<OrderViewModel> Orders { get; init; } = [];
}
