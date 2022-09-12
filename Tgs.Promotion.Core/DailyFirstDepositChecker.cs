namespace Tgs.Promotion;

internal class DailyFirstDepositChecker : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository;

	public DailyFirstDepositChecker(IPlayerDataRepository playerDataRepository)
	{
		_playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));
	}

	public ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
		=> _playerDataRepository.CheckPlayerIsDailyFirstDepositAsync(playerCode);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId)
		=> ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId)
		=> promotionId is null
			? ValueTask.CompletedTask
			: _playerDataRepository.RemovePlayerDailyDepositFlagAsync(playerCode);

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId)
		=> promotionId is null
			? ValueTask.CompletedTask
			: _playerDataRepository.SetPlayerDailyDepositFlagAsync(playerCode);

	public ValueTask<bool> CheckJoinAsync(ConditionData conditionData, PromotionMainInfo promotion, PromotionRule rule)
		=> CheckAsync(conditionData.PlayerCode, promotion);
}
