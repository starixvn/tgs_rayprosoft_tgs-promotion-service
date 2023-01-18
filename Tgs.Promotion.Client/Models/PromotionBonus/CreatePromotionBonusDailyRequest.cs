namespace Tgs.Promotion.Clients.Models.PromotionBonus;

public record CreatePromotionBonusDailyRequest(int TimeSpanMinutes, DateOnly? Date = null);
