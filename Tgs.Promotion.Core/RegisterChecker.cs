namespace Tgs.Promotion;

internal class RegisterChecker : IPromotionChecker
{
	private readonly IPlayerRepository _playerRepository;
	private readonly IPromotionRepository _promotionRepository;

	public RegisterChecker(IPlayerRepository playerRepository, IPromotionRepository promotionRepository)
	{
		_playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public async ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
	{
		var player = await _playerRepository
			.GetPlayerByPlayerCodeAsync(playerCode)
			.ConfigureAwait(false);

		if (player is null)
		{
			return false;
		}

		var promotion = await _promotionRepository.GetPromotionByIdAsync(promotionId!.Value).ConfigureAwait(false);

		if (promotion is null)
		{
			return false;
		}

		return player.Status == (short)PlayerStatus.Enabled && promotion.From <= player.RegisteredOn!.Value.Date &&
			promotion.To >= player.RegisteredOn!.Value.Date;
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
}