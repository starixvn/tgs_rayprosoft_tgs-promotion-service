
using System.Text.Json;

namespace Tgs.Promotion.Clients.Models.PromotionDistribute;

public class PromotionDistributeDetailModel
{
	public Guid BrandId { get; set; }
	public Guid PromotionId { get; set; }
	public Guid ReferenceId { get; set; }
	public JsonDocument? Detail { get; set; } = default!;
}
