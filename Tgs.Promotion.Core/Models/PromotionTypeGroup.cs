using Tgs.Promotion.Models;
namespace Tgs.Promotion.Models;

public record PromotionTypeGroup
{
	public string Name { get; set; } = default!;
	public PromotionType[] Types { get; set; } = default!;
	public Translation Trans { get; set; } = default!;
}
