using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.Service.ViewModels;

public class PromotionRuleViewModel
{
	public decimal BonusAmountLimit { get; init; }

	[JsonPropertyName("compaignDetail")]
	public JsonDocument CampaignDetail { get; init; } = default!;

	public GameType? GameType { get; init; }

	public decimal Percentage { get; init; }

	public decimal TurnoverMultiple { get; init; }

	public IEnumerable<int> VipLevels { get; set; } = [];
}
