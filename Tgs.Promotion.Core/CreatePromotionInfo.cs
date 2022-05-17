using System.Text.Json;

namespace Tgs.Promotion;

public class CreatePromotionInfo
{
	public Guid BrandId { get; init; }

	public string Name { get; init; } = default!;

	public (DateTime From, DateTime To) EventDuring { get; init; }

	public string PromotionType { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public bool IsAutoSent { get; init; }

	public JsonDocument CampaignDetail { get; init; } = default!;

	public string Modifier { get; init; } = default!;
}
