namespace Tgs.Promotion.Domain.Models.Promotion
{
    public class ChangeOrderRequest
    {
        public ChangeOrderModel[] Orders { get; set; } = default!;
    }
}
