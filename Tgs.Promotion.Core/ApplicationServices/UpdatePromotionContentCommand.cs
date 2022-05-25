using System.Text.Json;

namespace Tgs.Promotion.ApplicationServices;
public record UpdatePromotionContentCommand(
	Guid PromotionId,
	string? SubSubject,
	string? PersonalComputerBanner,
	string? MobileBanner,
	string? Content,
	string? Convention,
	JsonDocument Localization,
	string Modifier);
