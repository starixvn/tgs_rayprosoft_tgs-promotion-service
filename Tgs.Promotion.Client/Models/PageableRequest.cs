namespace Tgs.Promotion.Client.Models;

public record PageableRequest
{
	public int Page { get; set; }

	public int Rows { get; set; }

	public short OrderBy { get; set; }

	public bool IsDesc => OrderBy == (short)SortOrder.Descending;

	public string? SortBy { get; set; }

	public bool ShowAll { get; set; }

	public SortOption GetSortOption() => new(SortBy, IsDesc);
}
