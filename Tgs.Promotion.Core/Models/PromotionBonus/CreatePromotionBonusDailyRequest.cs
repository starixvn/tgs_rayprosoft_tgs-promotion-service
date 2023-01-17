namespace Tgs.Promotion.Models.PromotionBonus;

public class CreatePromotionBonusDailyRequest
{
	public DateOnly? Date { get; set; }

	public int TimeSpanMinutes { get; set; } = default!;
}
