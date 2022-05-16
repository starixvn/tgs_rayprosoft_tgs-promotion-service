namespace Tgs.Promotion.Client.Models.Promotion
{
	public record JoinedPromotionModel
	{
		public string PromotionName { get; set; } = default!;
		public DateTimeOffset CreatedOn { get; set; }
	}
}
