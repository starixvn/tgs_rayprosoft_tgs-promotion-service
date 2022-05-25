namespace Tgs.Promotion.Clients.Models.Promotion;

public record ChangeOrderRequest
{
	public ChangeOrderModel[] Orders { get; set; } = default!;
}
