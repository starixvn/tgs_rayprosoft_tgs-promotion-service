using System.Text.Json;

namespace Tgs.Promotion.Clients.Models.Promotion;

public record CreatePromotionRequest
{
	public Guid BrandId { get; set; }

	public string Name { get; set; } = default!;

	public DateTimeOffset From { get; set; }

	public DateTimeOffset To { get; set; }

	public short Status { get; set; }

	public string PromotionType { get; set; } = default!;

	public decimal BonusAmountLimit { get; set; }

	public bool Unlimited { get; set; }

	public short? GameType { get; set; }

	public decimal Percentage { get; set; }

	public decimal? TotalAmountLimit { get; set; }

	public decimal TurnoverMultiple { get; set; }

	public Guid[]? GameProviderIds { get; set; }

	public int[] VipLevels { get; set; } = default!;

	public string? EditorUsername { get; set; }

	public bool AutoSentPlayerBonus { get; set; }

	public JsonDocument? Localization { get; set; }

	public string SubSubject { get; set; } = default!;

	public string? Content { get; set; }

	public string? Terms { get; set; }

	public string? DesktopBanner { get; set; }

	public string? MobileBanner { get; set; }
}
