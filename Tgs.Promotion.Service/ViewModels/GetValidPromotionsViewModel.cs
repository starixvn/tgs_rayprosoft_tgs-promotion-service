using Tgs.Promotion.Models;

namespace Tgs.Promotion.Service.ViewModels;

public class GetValidPromotionsViewModel : PageableRequest
{
	public Guid BrandId { get; set; }

	public string? PlayerCode { get; set; }

	public string? Language { get; set; }
}
