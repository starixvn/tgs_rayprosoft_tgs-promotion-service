namespace Tgs.Promotion.Domain.Models
{
	public record Translation
	{
		public string En { get; set; } = default!;

		public string Zh { get; set; } = default!;
	}
}
