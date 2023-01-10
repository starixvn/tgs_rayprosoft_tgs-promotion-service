namespace Tgs.Promotion.Clients.Models.PromotionBonus;

public record CreatePromotionBonusMonthlyRequest(int TimeSpanMinutes, string? From = null, string? To = null);
