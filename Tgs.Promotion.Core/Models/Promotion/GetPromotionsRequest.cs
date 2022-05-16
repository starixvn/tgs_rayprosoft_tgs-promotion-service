namespace Tgs.Promotion.Core.Models.Promotion
{
	public class GetPromotionsRequest : PageableRequest
	{
		public Guid BrandId { get; set; }

		public string? Name { get; set; }

		public PromotionStatus? Status { get; set; }

		public string? Type { get; set; }

		public DateTimeOffset? From { get; set; }

		public DateTimeOffset? To { get; set; }
	}
}
