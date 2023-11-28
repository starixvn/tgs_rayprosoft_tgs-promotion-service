namespace Tgs.Promotion.Workers;

public class DispatchPlayerQueueModel
{
	public string PlayerCode { get; set; } = default!;

	public Guid PromotionBonusId { get; set; }
}
