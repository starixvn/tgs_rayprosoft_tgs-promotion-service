namespace Tgs.Promotion.Service.MessageQueue;
public record TurnoverRebateWeeklySettings(
	GameType? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	decimal TurnoverLimit,
	bool PlayerAutoJoin);
