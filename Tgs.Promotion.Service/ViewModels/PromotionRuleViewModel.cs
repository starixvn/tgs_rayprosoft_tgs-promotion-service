using System.Text.Json;

namespace Tgs.Promotion.Service.ViewModels;

public class PromotionRuleViewModel
{
	public decimal BonusAmountLimit { get; init; }

	public JsonDocument CompaignDetail { get; init; } = default!;

	public GameType? GameType { get; init; }

	public decimal Percentage { get; init; }

	public decimal TurnoverMultiple { get; init; }
}
