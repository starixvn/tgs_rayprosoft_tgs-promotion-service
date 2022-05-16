using Tgs.Promotion.Domain;

namespace Tgs.Promotion.Core
{
	internal class FirstDepositChecker : IPromotionChecker
	{
		private readonly IPlayerDataRepository _playerDataRepository;
		private readonly IPlayerRepository _playerService;

		public FirstDepositChecker(IPlayerRepository playerService, IPlayerDataRepository playerDataRepository)
		{
			_playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
			_playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));
		}

		public async ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
		{
			var isFirstDepositing = await _playerDataRepository
				.CheckPlayerFirstDepositingAsync(playerCode)
				.ConfigureAwait(false);

			if (isFirstDepositing)
				return false;

			var player = await _playerService
				.GetPlayerByPlayerCodeAsync(playerCode)
				.ConfigureAwait(false);

			if (player == null)
				return false;

			return player.FirstDepositOn == null;
		}

		public ValueTask CommitTagAsync(string playerCode, Guid? promotionId)
			=> _playerDataRepository
				.RemovePlayerFirstDepositFlagAsync(playerCode);

		public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId)
			=> _playerDataRepository
				.RemovePlayerFirstDepositFlagAsync(playerCode);

		public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId)
			=> _playerDataRepository.SetPlayerFirstDepositFlagAsync(playerCode);
	}
}
