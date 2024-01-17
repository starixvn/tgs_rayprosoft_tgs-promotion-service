using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class EventDetailViewModel
{
	public PromotionEventAction Action { get; init; } = default!;

	public JsonDocument ChangeSets { get; init; } = default!;

	public DateTime CreatedOn { get; init; }

	public Guid EventId { get; init; }

	public string? Modifier { get; init; }

	public Guid PromotionId { get; init; }
}
