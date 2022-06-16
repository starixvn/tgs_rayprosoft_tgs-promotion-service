namespace Tgs.Promotion.Clients.Models;

public class SortOption
{
	public bool IsDescending { get; init; }

	public string? PropertyName { get; init; }

	public SortOption(string? propertyName, bool isDescending)
	{
		IsDescending = isDescending;
		PropertyName = propertyName;
	}
}
