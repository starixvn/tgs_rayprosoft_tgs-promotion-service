namespace Tgs.Promotion.Client.Models.Promotion
{
    public record GetJoinedPromotionsRequest : PageableRequest
    {
        public string PlayerCode { get; set; } = default!;
        public DateTimeOffset From { get; init; }
        public DateTimeOffset To { get; init; }
    }
}