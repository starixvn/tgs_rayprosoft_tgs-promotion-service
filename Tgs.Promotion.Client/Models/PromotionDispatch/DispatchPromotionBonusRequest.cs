namespace Tgs.Promotion.Client.Models.PromotionDispatch
{
	public class DispatchPromotionBonusRequest
	{
		public Guid OperatorUserId { get; set; }

		public DispatchPlayer[] Players { get; set; } = default!;
	}

	public class DispatchPlayer
	{
		public Guid PromotionBonusId { get; set; }

		public string PlayerCode { get; set; } = default!;
	}
}
