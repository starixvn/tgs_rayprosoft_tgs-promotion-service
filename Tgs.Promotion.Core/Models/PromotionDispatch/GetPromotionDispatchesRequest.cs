namespace Tgs.Promotion.Core.Models.PromotionDispatch;

public class GetPromotionDispatchesRequest : PageableRequest
{
	public Guid BrandId { get; set; }

	public DateTimeOffset? From { get; set; }

	public string? PromotionGroup { get; set; }

	public string? PromotionName { get; set; }

	public DateTimeOffset? To { get; set; }

	public string? Username { get; set; }
}
