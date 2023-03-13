namespace Tgs.Promotion;
public record GameProviderSettings(
	GameType? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple);
