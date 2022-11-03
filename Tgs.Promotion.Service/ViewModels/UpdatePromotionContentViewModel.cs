using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class UpdatePromotionContentViewModel
{
	public string? Content { get; init; }

	public string? Convention { get; init; }

	public JsonDocument Localization { get; init; } = default!;

	public string? MobileBanner { get; init; }

	public string Modifier { get; init; } = default!;

	public string? PersonalComputerBanner { get; init; }

	public string? SubSubject { get; init; }
}
