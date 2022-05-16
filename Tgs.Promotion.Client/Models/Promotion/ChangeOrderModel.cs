namespace Tgs.Promotion.Client.Models.Promotion
{
	public record ChangeOrderModel
	{
		public int DisplayOrder { get; set; }

		public Guid Id { get; set; }
	}
}
