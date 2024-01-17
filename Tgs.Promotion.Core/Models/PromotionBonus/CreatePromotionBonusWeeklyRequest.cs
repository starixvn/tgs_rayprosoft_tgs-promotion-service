namespace Tgs.Promotion.Models.PromotionBonus;

public class CreatePromotionBonusWeeklyRequest
{
	public DateOnly? From { get; set; }

	public int TimeSpanMinutes { get; set; }

	public DateOnly? To { get; set; }
}
