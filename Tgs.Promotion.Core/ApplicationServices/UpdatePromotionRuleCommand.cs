using System.Text.Json;

namespace Tgs.Promotion.ApplicationServices;
public record UpdatePromotionRuleCommand(
	Guid PromotionId,
	JsonDocument CampaignDetail,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	GameType? GameType,
	string Modifier);
