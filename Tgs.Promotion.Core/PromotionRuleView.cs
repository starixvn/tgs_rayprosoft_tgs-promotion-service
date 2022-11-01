using System.Text.Json;

namespace Tgs.Promotion;

public record PromotionRuleView(
	JsonDocument CampaignDetail,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	GameType? GameType,
	IEnumerable<int> VipLevels);
