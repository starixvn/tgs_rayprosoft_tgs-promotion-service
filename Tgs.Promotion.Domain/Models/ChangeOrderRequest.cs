using Tgs.Promotion.Domain.Models.Promotion;

namespace TGS.Services.Promotion.Models.Promotion
{
    public class ChangeOrderRequest
    {
        public ChangeOrderModel[] Orders { get; set; } = default!;
    }
}
