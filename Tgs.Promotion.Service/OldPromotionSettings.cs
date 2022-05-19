namespace Tgs.Promotion.Service;

internal record OldPromotionSettings(
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple);
