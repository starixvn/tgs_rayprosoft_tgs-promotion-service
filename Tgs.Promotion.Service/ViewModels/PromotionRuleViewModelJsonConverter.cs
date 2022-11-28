using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.Service.ViewModels;

public class PromotionRuleViewModelJsonConverter : JsonConverter<PromotionRuleViewModel>
{
	private static readonly JsonSerializerOptions _InternalSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

	public override PromotionRuleViewModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var jsonDoc = JsonSerializer.Deserialize<JsonNode>(ref reader, options);

		if (jsonDoc is null)
			return null;

		if (jsonDoc["compaignDetail"] is not null && jsonDoc["campaignDetail"] is null)
			jsonDoc["campaignDetail"] = JsonNode.Parse(jsonDoc["compaignDetail"]!.ToJsonString());

		return jsonDoc.Deserialize<PromotionRuleViewModel>(_InternalSerializerOptions);
	}

	public override void Write(Utf8JsonWriter writer, PromotionRuleViewModel value, JsonSerializerOptions options)
	{
		writer.WriteRawValue(JsonSerializer.Serialize(value, _InternalSerializerOptions));
	}
}
