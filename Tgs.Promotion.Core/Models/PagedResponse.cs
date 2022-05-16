namespace Tgs.Promotion.Core.Models;

public record PagedResponse<T> : MessageResponse
{
	public PagedResponse(int count, T[] items)
	{
		TotalCount = count;
		Items = items;
	}

	public int TotalCount { get; set; }
	public T[] Items { get; set; }
}
