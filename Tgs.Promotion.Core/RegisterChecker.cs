using Tgs.PlayerService.Client;

namespace Tgs.Promotion;

internal class RegisterChecker : IPromotionChecker
{
	private readonly IPlayerServiceClient _playerServiceClient;

	public RegisterChecker(IPlayerServiceClient playerServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoAsync(playerCode)
			.ConfigureAwait(false);

		return player is not null
			&& player.Status == (short)PlayerStatus.Enabled
			&& promotion.EventDuring.From <= player.RegisteredOn!.Value.Date
			&& promotion.EventDuring.To >= player.RegisteredOn!.Value.Date;
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
}
