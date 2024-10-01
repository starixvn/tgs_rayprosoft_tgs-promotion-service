
namespace Tgs.Promotion.Clients.Models.PromotionDistribute;

public class PromotionDistributeModel
{
	public decimal BonusAmount { get; set; }

	public DateTimeOffset CreatedOn { get; set; }

	public string PlayerCode { get; set; } = default!;

	public Guid Id { get; set; }

	public Guid PromotionId { get; set; }

	public string PromotionName { get; set; } = default!;

	public string PromotionType { get; set; } = default!;

	public DateTimeOffset? SentOn { get; set; }

	public Guid ReferenceId { get; set; }
}
