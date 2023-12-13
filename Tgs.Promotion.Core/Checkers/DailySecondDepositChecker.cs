using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class DailySecondDepositChecker(IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));

	public async ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
		=> !await _playerDataRepository
			.CheckPlayerIsDailyFirstDepositAsync(playerCode)
			.ConfigureAwait(false);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
