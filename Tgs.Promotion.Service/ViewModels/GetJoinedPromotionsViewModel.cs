using Tgs.Promotion.Models;

namespace Tgs.Promotion.Service.ViewModels;

public class GetJoinedPromotionsViewModel : PageableRequest
{
	public DateTimeOffset From { get; init; }

	public string PlayerCode { get; init; } = default!;

	public DateTimeOffset To { get; init; }
}
