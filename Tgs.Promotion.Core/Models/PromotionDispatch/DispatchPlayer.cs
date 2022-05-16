namespace Tgs.Promotion.Core.Models.PromotionDispatch
{
	public class DispatchPlayer
	{
		public string PlayerCode { get; set; } = default!;

		public Guid PromotionBonusId { get; set; }
	}
}
