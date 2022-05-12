namespace Tgs.Promotion.Client.Models.Promotion
{
    public record ChangeOrderRequest
    {
        public ChangeOrderModel[] Orders { get; set; } = default!;
    }
}
