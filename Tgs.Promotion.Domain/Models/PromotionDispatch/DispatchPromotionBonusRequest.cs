namespace Tgs.Promotion.Domain.Models.PromotionDispatch
{
	public class DispatchPromotionBonusRequest
	{
		public Guid OperatorUserId { get; set; }

		public DispatchPlayer[] Players { get; set; } = default!;
	}
}
