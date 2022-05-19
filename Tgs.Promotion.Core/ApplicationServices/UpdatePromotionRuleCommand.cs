using System.Text.Json;

namespace Tgs.Promotion.ApplicationServices;
public record UpdatePromotionRuleCommand(
	Guid PromotionId,
	JsonDocument CampaignDetail,
	string Modifier);
