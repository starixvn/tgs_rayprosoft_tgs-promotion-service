namespace Tgs.Promotion.Service.ViewModels;

public class VipLevelsViewModel
{
	public string Modifier { get; init; } = default!;

	public IEnumerable<int> VipLevels { get; init; } = Array.Empty<int>();
}
