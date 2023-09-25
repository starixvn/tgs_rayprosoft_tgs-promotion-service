using Microsoft.AspNetCore.Mvc;

namespace Tgs.Promotion.Service.ViewModels;

public class ListPromotionByFiltersViewModel
{
	public DateTime? From { get; init; }

	public string? Name { get; init; }

	[FromQuery(Name = "promotionType")]
	public IEnumerable<string>? PromotionTypes { get; init; }

	public PromotionStatus? Status { get; init; }

	public DateTime? To { get; init; }
}
