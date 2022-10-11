namespace Tgs.Promotion.Service.MessageQueue;
public record TurnoverRebateDailySettings(
	GameType? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	decimal TurnoverLimit,
	bool PlayerAutoJoin);
