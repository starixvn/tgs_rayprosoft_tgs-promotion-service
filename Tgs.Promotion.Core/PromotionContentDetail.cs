using System.Text.Json;

namespace Tgs.Promotion;
public record PromotionContentDetail(
	string? SubSubject,
	string? PersonalComputerBanner,
	string? MobileBanner,
	string? Content,
	string? Convention,
	JsonDocument Localization);
