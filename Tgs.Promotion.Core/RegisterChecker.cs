using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.shared;

namespace Tgs.Promotion.Core
{
    internal class RegisterChecker : IPromotionChecker
    {
        private readonly IPromotionParticipantRepository _promotionParticipantRepository;
        private readonly IPlayerRepository _playerService;

        public RegisterChecker(IPlayerRepository playerService, IPromotionParticipantRepository promotionParticipantRepository)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _promotionParticipantRepository = promotionParticipantRepository ?? throw new ArgumentNullException(nameof(promotionParticipantRepository));
        }

        public async ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
        {
            var player = await _playerService
                .GetPlayerByPlayerCodeAsync(playerCode)
                .ConfigureAwait(false);

            if (player is null)
                return false;

            var isExists = await _promotionParticipantRepository.ExistsAsync(x
                => x.PlayerCode == playerCode && x.PromotionId == promotionId);

            return player.Status == (short)PlayerStatus.Enabled && !isExists;
        }

        public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

        public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

        public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
    }
}
