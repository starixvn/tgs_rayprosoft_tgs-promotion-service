using System.Text.Json;

namespace Tgs.Promotion;

public record EventInfo(
	Guid EventId,
	Guid PromotionId,
	PromotionEventAction Action,
	JsonDocument ChangeSets,
	string? Modifier,
	DateTime CreatedOn);
