namespace Tgs.Promotion.Models.Promotion;

public class GetJoinedPromotionsRequest : PageableRequest
{
	public DateTimeOffset From { get; init; }

	public string PlayerCode { get; init; } = default!;

	public DateTimeOffset To { get; init; }
}
