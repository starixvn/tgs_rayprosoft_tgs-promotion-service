using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class PromotionListItemViewModel
{
	public JsonDocument CampaignDetail { get; set; } = default!;

	public string? Content { get; set; }

	public string? Convention { get; set; }

	public DuringViewModel EventDuring { get; set; }

	public Guid Id { get; set; }

	public JsonDocument Localization { get; set; } = default!;

	public string? MobileBanner { get; set; }

	public string Name { get; set; } = default!;

	public string? PersonalComputerBanner { get; set; }

	public decimal? PromotionLimitAmount { get; set; }

	public string PromotionType { get; set; } = default!;

	public string? SubSubject { get; set; }

	public decimal UsedAmount { get; set; }

	public IEnumerable<int> VipLevels { get; set; } = Array.Empty<int>();
}
