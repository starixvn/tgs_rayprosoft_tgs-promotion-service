using System.Text.Json;

namespace Tgs.Promotion.Clients;

public record PromotionEventDetailInfo
{
	public JsonDocument ChangeSets { get; init; } = default!;
}
