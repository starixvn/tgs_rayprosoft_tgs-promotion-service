namespace Tgs.Promotion.Clients.Models.PromotionBonus;

public record CreatePromotionBonusWeeklyRequest(int TimeSpanMinutes, DateOnly? From = null, DateOnly? To = null);
