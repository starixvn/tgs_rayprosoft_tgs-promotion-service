using Tgs.Promotion.Domain.Interfaces;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Services
{
    internal class TagPlayerDepositCommandService : ICommandService<TagPlayerDepositCommand>
    {
        private readonly IPromotionChecker[] _checkers;

        public TagPlayerDepositCommandService(IPromotionCheckerFactory promotionCheckerFactory)
        {
            if (promotionCheckerFactory is null)
            {
                throw new ArgumentNullException(nameof(promotionCheckerFactory));
            }

            var firstDepositChecker = promotionCheckerFactory.GetPromotionChecker("FirstDeposit");
            var AccountfirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("AccountFirstDeposit");
            var dailyFirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("DailyFirstDeposit");

            _checkers = new[] { firstDepositChecker, AccountfirstDepositChecker, dailyFirstDepositChecker };
        }

        public async ValueTask ExecuteAsync(TagPlayerDepositCommand command, CancellationToken cancellationToken = default)
        {
            foreach (var checker in _checkers)
                await checker
                    .TagPromotionTypeAsync(command.PlayerCode, command.PromotionId)
                    .ConfigureAwait(false);
        }
    }
}
