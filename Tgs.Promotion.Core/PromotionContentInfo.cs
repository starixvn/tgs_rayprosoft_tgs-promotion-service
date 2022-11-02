using System.Text.Json;

namespace Tgs.Promotion;

public record PromotionContentInfo(
 string? Content,
 string? Convention,
 string? SubSubject,
 JsonDocument Localization,
 string? PersonalComputerBanner,
 string? MobileBanner);
