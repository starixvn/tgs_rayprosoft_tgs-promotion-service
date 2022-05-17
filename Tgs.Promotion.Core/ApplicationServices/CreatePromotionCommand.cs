using System.Text.Json;

namespace Tgs.Promotion.ApplicationServices;

public record CreatePromotionCommand(
	Guid BrandId,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	bool IsAutoSent,
	JsonDocument CampaignDetail,
	string Modifier);
