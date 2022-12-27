using Tgs.Promotion.Models;

namespace Tgs.Promotion.ApplicationServices;

public class GetPromotionsQuery : PageableRequest
{
	public Guid BrandId { get; set; }

	public DateTimeOffset? From { get; set; }

	public string? Name { get; set; }

	public PromotionStatus? Status { get; set; }

	public DateTimeOffset? To { get; set; }

	public string? Type { get; set; }
}
