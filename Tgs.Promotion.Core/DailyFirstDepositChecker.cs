namespace Tgs.Promotion.Core;

internal class DailyFirstDepositChecker : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository;

	public DailyFirstDepositChecker(IPlayerDataRepository playerDataRepository)
	{
		_playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));
	}

	public ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
		=> _playerDataRepository.CheckPlayerIsDailyFirstDepositAsync(playerCode);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId)
		=> ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId)
	{
		if (promotionId is null)
			return ValueTask.CompletedTask;

		return _playerDataRepository.RemovePlayerDailyDepositFlagAsync(playerCode);
	}

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId)
	{
		if (promotionId is null)
			return ValueTask.CompletedTask;

		return _playerDataRepository.SetPlayerDailyDepositFlagAsync(playerCode);
	}
}
