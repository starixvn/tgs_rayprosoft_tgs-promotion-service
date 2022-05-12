using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.Core
{
    internal class FirstJoinPromotionChecker : IPromotionChecker
    {
        private readonly IPlayerDataRepository _playerDataRepository;
        private readonly IPlayerRepository _playerService;

        public FirstJoinPromotionChecker(IPlayerRepository playerService, IPlayerDataRepository playerDataRepository)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));
        }

        public async ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
        {
            var isFirstDepositing = await _playerDataRepository
                .CheckPlayerFirstJoinPromotionAsync(playerCode)
                .ConfigureAwait(false);

            if (isFirstDepositing)
                return false;

            var player = await _playerService
                .GetPlayerByPlayerCodeAsync(playerCode)
                .ConfigureAwait(false);

            if (player is null)
                return false;

            return player.FirstPromotionOn == null;
        }

        public async ValueTask CommitTagAsync(string playerCode, Guid? promotionId)
        {
            if (promotionId is null)
                return;

            await _playerService
                .SetFirstPromotionTimeAsync(playerCode, DateTimeOffset.UtcNow)
                .ConfigureAwait(false);

            await _playerDataRepository
                .RemovePlayerFirstJoinPromotionAsync(playerCode)
                .ConfigureAwait(false);
        }

        public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId)
        {
            if (promotionId is null)
                return ValueTask.CompletedTask;

            return _playerDataRepository
                .RemovePlayerFirstJoinPromotionAsync(playerCode);
        }

        public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId)
        {
            if (promotionId == null)
                return ValueTask.CompletedTask;

            return _playerDataRepository
                .SetPlayerFirstJoinPromotionAsync(playerCode);
        }
    }
}
