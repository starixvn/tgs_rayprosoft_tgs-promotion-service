namespace Tgs.Promotion.Clients.Models.Promotion;

public record GetPromotionsRequest : PageableRequest
{
	public Guid BrandId { get; set; }
	public string? Type { get; set; }
	public short? Status { get; set; }
	public string? Name { get; set; }
	public DateTimeOffset? From { get; set; }
	public DateTimeOffset? To { get; set; }
}
