using Tgs.Promotion.Models;

namespace Tgs.Promotion.ApplicationServices;

public class GetJoinedPromotionsQuery : PageableRequest
{
	public DateTimeOffset From { get; init; }

	public string PlayerCode { get; init; } = default!;

	public DateTimeOffset To { get; init; }
}
