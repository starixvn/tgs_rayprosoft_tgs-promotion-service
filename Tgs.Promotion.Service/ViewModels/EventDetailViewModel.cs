using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class EventDetailViewModel
{
	public Guid EventId { get; init; }

	public Guid PromotionId { get; init; }

	public PromotionEventAction Action { get; init; } = default!;

	public string? Modifier { get; init; }

	public DateTime CreatedOn { get; init; }

	public JsonDocument ChangeSets { get; init; } = default!;
}
