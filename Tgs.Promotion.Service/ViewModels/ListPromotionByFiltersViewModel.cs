namespace Tgs.Promotion.Service.ViewModels;

public class ListPromotionByFiltersViewModel
{
	public DateTime? From { get; init; }

	public string? Name { get; init; }

	public string? PromotionType { get; init; }

	public PromotionStatus? Status { get; init; }

	public DateTime? To { get; init; }
}
