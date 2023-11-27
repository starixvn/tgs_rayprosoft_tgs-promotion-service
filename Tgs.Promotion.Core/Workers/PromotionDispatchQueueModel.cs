namespace Tgs.Promotion.Workers;

public class PromotionDispatchQueueModel
{
	public Guid OperatorUserId { get; set; }

	public DispatchPlayerQueueModel[] Players { get; set; } = default!;
}