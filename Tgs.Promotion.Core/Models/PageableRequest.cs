namespace Tgs.Promotion.Models;

public class PageableRequest
{
	public bool IsDesc => OrderBy == SortOrder.Descending;

	public SortOrder OrderBy { get; set; }

	public int Page { get; set; }

	public int Rows { get; set; }

	public bool ShowAll { get; set; }

	public string? SortBy { get; set; }

	public SortOption GetSortOption() => new(SortBy, IsDesc);
}
