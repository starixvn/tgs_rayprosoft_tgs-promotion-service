namespace Tgs.Promotion.Service.ViewModels;

public class PromotionViewModel
{
	public DuringViewModel EventDuring { get; init; }

	public Guid Id { get; init; }

	public bool IsAutoSent { get; init; }

	public string Name { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public string PromotionType { get; init; } = default!;
}
