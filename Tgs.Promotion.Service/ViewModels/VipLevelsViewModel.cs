namespace Tgs.Promotion.Service.ViewModels;

public class VipLevelsViewModel
{
	public IEnumerable<int> VipLevels { get; init; } = Array.Empty<int>();

	public string Modifier { get; init; } = default!;
}
