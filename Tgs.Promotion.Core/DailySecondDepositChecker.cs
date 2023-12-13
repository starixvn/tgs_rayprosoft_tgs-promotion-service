namespace Tgs.Promotion;

internal class DailySecondDepositChecker(IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));

	public async ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
		=> !await _playerDataRepository
			.CheckPlayerIsDailyFirstDepositAsync(playerCode)
			.ConfigureAwait(false);

	public ValueTask<bool> CheckJoinAsync(ConditionData conditionData, PromotionMainInfo promotion, PromotionRule rule)
		=> CheckAsync(conditionData.PlayerCode, promotion);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
}
