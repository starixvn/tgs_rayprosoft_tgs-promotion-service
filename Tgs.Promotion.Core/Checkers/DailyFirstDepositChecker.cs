using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class DailyFirstDepositChecker(IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));

	public ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
		=> _playerDataRepository.CheckPlayerIsDailyFirstDepositAsync(playerCode);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
			=> ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> promotionId is null
			? ValueTask.CompletedTask
			: _playerDataRepository.RemovePlayerDailyDepositFlagAsync(playerCode);

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> promotionId is null
			? ValueTask.CompletedTask
			: _playerDataRepository.SetPlayerDailyDepositFlagAsync(playerCode);
}
