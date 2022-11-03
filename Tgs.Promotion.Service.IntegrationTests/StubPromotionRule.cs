namespace Tgs.Promotion.Service.IntegrationTests;
internal record StubPromotionRule
{
	public decimal BonusAmountLimit { get; init; }

	public StubPromotionRuleDetail CompaignDetail { get; init; } = default!;

	public GameType GameType { get; init; }

	public decimal Percentage { get; init; }

	public decimal TurnoverMultiple { get; init; }

	public IEnumerable<int> VipLevels { get; init; } = Array.Empty<int>();
}
