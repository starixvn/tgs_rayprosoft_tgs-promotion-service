
namespace Tgs.Promotion.ApplicationServices.Promotion;
internal class TurnoverRebateWeeklyRule
{
	public decimal TurnoverLimit { get; set; }
	public decimal Percentage { get; set; }
	public decimal BonusAmountLimit { get; set; }
	public decimal TurnoverMultiple { get; set; }
}
