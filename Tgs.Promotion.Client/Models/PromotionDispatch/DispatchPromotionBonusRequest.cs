namespace Tgs.Promotion.Clients.Models.PromotionDispatch;

public class DispatchPromotionBonusRequest
{
	public Guid OperatorUserId { get; set; }

	public DispatchPlayer[] Players { get; set; } = default!;
}

public class DispatchPlayer
{
	public string PlayerCode { get; set; } = default!;

	public Guid PromotionBonusId { get; set; }
}
