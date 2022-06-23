using System.Text.Json;

namespace Tgs.Promotion;
public record PromotionView(
	Guid Id,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	string PromotionType,
	decimal? PromotionLimitAmount,
	decimal UsedAmount,
	IEnumerable<int> VipLevels,
	string? SubSubject,
	string? PersonalComputerBanner,
	string? MobileBanner,
	string? Content,
	string? Convention,
	JsonDocument Localization,
	JsonDocument CampaignDetail);
