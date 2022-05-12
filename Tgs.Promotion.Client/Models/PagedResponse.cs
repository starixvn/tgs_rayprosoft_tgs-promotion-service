namespace Tgs.Promotion.Client.Models
{
    public record PagedResponse<T> : MessageResponse
    {
        public int TotalCount { get; init; }
        public T[] Items { get; init; }

        public PagedResponse() => Items = Array.Empty<T>();

        public PagedResponse(int count, T[] items)
        {
            TotalCount = count;
            Items = items;
        }
    }
}