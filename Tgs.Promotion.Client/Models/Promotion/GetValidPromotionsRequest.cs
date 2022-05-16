namespace Tgs.Promotion.Client.Models.Promotion;

public record GetValidPromotionsRequest : PageableRequest
{
	public Guid BrandId { get; set; }
	public string? PlayerCode { get; set; }
	public string? Language { get; set; }
}
