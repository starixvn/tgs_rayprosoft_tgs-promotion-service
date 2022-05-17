using Tgs.Promotion;
namespace Tgs.Promotion;

internal class DailySecondDepositChecker : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository;

	public DailySecondDepositChecker(IPlayerDataRepository playerDataRepository)
	{
		_playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));
	}

	public async ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
	{
		return !await _playerDataRepository
			.CheckPlayerIsDailyFirstDepositAsync(playerCode)
			.ConfigureAwait(false);
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
}
