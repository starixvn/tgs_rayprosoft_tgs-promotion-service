using Tgs.Promotion.Core.Models.Promotion;

namespace Tgs.Promotion.Core.ApplicationServices;

public record ChangeOrderCommand
{
	public ChangeOrderModel[] Orders { get; set; } = default!;
}
