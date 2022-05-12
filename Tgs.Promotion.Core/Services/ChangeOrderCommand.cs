using Tgs.Promotion.Domain.Models.Promotion;

namespace Tgs.Promotion.Core.Services
{
    public record ChangeOrderCommand
    {
        public ChangeOrderModel[] Orders { get; set; } = default!;
    }
}
