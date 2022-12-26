namespace Tgs.Promotion.ApplicationServices;

public class DispatchPromotionBonusQuery
{
	public Guid OperatorUserId { get; set; }

	public DispatchPlayer[] Players { get; set; } = default!;
}
