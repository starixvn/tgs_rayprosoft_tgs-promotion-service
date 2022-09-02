
namespace Tgs.Promotion.ApplicationServices.Promotion;
internal class TurnoverRebateWeeklyRule
{
	public GameType? GameType { get; set; }
	public IEnumerable<Guid> GameProviderIds { get; set; }
	public decimal TurnoverLimit { get; set; }
	public decimal Percentage { get; set; }
	public decimal BonusAmountLimit { get; set; }
	public decimal TurnoverMultiple { get; set; }
}
