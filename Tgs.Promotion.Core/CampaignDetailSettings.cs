namespace Tgs.Promotion;
public record CampaignDetailSettings(
	GameType? GameType,
	IEnumerable<Guid> GameProviderIds,
	decimal BonusAmountLimit,
	decimal Percentage,
	decimal TurnoverMultiple,
	IEnumerable<int> VipLevels);
