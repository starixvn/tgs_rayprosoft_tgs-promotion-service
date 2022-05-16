using Tgs.Promotion.Core.Models;

namespace Tgs.Promotion.Service.ViewModels
{
	public class GetValidPromotionsRequest : PageableRequest
	{
		public Guid BrandId { get; set; }

		public string? PlayerCode { get; set; }

		public string? Language { get; set; }
	}
}
