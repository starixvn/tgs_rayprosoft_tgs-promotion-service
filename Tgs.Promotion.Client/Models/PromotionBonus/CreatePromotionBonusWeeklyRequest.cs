namespace Tgs.Promotion.Clients.Models.PromotionBonus;

public record CreatePromotionBonusWeeklyRequest(int TimeSpanMinutes, string? From = null, string? To = null);
