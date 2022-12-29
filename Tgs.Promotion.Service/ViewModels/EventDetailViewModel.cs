using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class EventDetailViewModel
{
	public JsonDocument ChangeSets { get; init; } = default!;
}
