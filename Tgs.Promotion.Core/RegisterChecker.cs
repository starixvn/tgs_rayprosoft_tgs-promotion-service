using Tgs.PlayerService.Client;

namespace Tgs.Promotion;

internal class RegisterChecker : IPromotionChecker
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPromotionParticipantRepository _promotionParticipantRepository;

	public RegisterChecker(IPlayerServiceClient playerServiceClient, IPromotionParticipantRepository promotionParticipantRepository)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_promotionParticipantRepository = promotionParticipantRepository ?? throw new ArgumentNullException(nameof(promotionParticipantRepository));
	}

	public async ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoAsync(playerCode)
			.ConfigureAwait(false);

		if (player is null)
			return false;

		var playerStatus = await _playerServiceClient
			.GetPlayerStatusAsync(playerCode)
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

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
	public ValueTask<bool> CheckJoinAsync(ConditionData conditionData, PromotionMainInfo promotion, PromotionRule rule)
	{
		return CheckAsync(conditionData.PlayerCode, promotion);
	}
}
