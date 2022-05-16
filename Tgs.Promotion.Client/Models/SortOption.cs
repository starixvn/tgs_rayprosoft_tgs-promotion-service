namespace Tgs.Promotion.Client.Models;

public class SortOption
{
	public string? PropertyName { get; init; }

	public bool IsDescending { get; init; }

	public SortOption(string? propertyName, bool isDescending)
	{
		IsDescending = isDescending;
		PropertyName = propertyName;
	}
}
