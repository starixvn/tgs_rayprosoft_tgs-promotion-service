using System.Text.Json;
using Tgs.PlayerService.Client;

namespace Tgs.Promotion.Checkers;

internal class RegisterChecker(IPlayerServiceClient playerServiceClient, IPromotionParticipantRepository promotionParticipantRepository) : IPromotionChecker
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	private readonly IPromotionParticipantRepository _promotionParticipantRepository = promotionParticipantRepository ?? throw new ArgumentNullException(nameof(promotionParticipantRepository));

	public async ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			return false;

		var playerStatus = await _playerServiceClient
			.GetPlayerStatusAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		if (playerStatus is not PlayerService.Client.PlayerStatus.Enabled)
			return false;

		var isExists = await _promotionParticipantRepository.ExistsAsync(x
			=> x.PlayerCode == playerCode && x.PromotionId == promotion.Id)
			.ConfigureAwait(false);

		return !isExists
			&& promotion.EventDuring.From <= player.RegisteredOn!.Value
			&& promotion.EventDuring.To >= player.RegisteredOn!.Value;
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
