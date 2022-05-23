using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class UpdatePromotionRuleViewModel
{
	public JsonDocument CampaignDetail { get; init; } = default!;

	public string Modifier { get; init; } = default!;
}
