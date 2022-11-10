using System.Text.Json;

namespace Tgs.Promotion;
public record PromotionDistributeLog(Guid Id, Guid DistributeId, DistributeAction Action, JsonDocument ChangeSets);
