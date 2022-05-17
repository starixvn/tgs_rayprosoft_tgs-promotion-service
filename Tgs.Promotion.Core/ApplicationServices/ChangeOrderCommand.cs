using Tgs.Promotion.Models.Promotion;

namespace Tgs.Promotion.ApplicationServices;

public record ChangeOrderCommand
{
	public ChangeOrderModel[] Orders { get; set; } = default!;
}
