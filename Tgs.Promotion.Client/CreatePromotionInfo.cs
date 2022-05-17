using System.Text.Json;

namespace Tgs.Promotion.Client;

public record CreatePromotionInfo<TCampaignDetail>
{
	public string Name { get; init; } = default!;

	public During EventDuring { get; init; }

	public string PromotionType { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public bool IsAutoSent { get; init; }

	public TCampaignDetail CampaignDetail { get; init; } = default!;
}
