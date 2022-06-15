namespace Tgs.Promotion.Service.Controllers;

public class PromotionBannerViewModel
{
	public IReadOnlyDictionary<string, string> Mobile { get; init; } = default!;

	public IReadOnlyDictionary<string, string> PersonalComputer { get; init; } = default!;
}
