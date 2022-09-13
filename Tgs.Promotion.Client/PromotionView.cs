using System.Text.Json;

namespace Tgs.Promotion.Clients;

public record PromotionView
{
	public string? Content { get; init; }

	public string? Convention { get; init; }

	public During EventDuring { get; init; }

	public Guid Id { get; init; }

	public JsonDocument Localization { get; init; } = default!;

	public string? MobileBanner { get; init; }

	public string Name { get; init; } = default!;

	public string? PersonalComputerBanner { get; init; }

	public decimal? PromotionLimitAmount { get; init; }

	public string PromotionType { get; init; } = default!;

	public string? SubSubject { get; init; }

	public decimal UsedAmount { get; init; }

	public IEnumerable<int> VipLevels { get; init; } = Array.Empty<int>();

	public JsonDocument CampaignDetail { get; init; } = default!;

	public int Order { get; init; }
}
