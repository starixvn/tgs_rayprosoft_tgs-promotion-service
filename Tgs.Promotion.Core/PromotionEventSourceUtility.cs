using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion;

internal static class PromotionEventSourceUtility
{
	public static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter() }
	};
}
