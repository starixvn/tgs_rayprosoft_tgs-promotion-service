namespace Tgs.Promotion.Service.ViewModels;

public class CreatePromotionViewModel
{
	public DuringViewModel EventDuring { get; init; }

	public bool IsAutoSent { get; init; }

	public string Modifier { get; init; } = default!;

	public string Name { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public string PromotionType { get; init; } = default!;
}
