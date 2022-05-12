namespace Tgs.Promotion.Client.Models.PromotionDispatch
{
    public record GetPromotionDispatchesRequest : PageableRequest
    {
        public Guid BrandId { get; set; }
        public string? PromotionGroup { get; set; }
        public string? PromotionName { get; set; }
        public string? Username { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
    }
}