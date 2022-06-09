namespace Tgs.Promotion.Service.ViewModels;

public class PromotionViewModel
{
	public Guid Id { get; init; }

	public string Name { get; init; } = default!;

	public DuringViewModel EventDuring { get; init; }

	public string PromotionType { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public bool IsAutoSent { get; init; }

	public string Modifier { get; init; } = default!;
}
