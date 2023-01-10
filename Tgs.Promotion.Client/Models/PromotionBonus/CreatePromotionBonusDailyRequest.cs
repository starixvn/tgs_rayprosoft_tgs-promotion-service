namespace Tgs.Promotion.Clients.Models.PromotionBonus;

public record CreatePromotionBonusDailyRequest(int TimeSpanMinutes, string? Date = null);
