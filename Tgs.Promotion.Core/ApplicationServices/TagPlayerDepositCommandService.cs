using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class TagPlayerDepositCommandService : ICommandService<TagPlayerDepositCommand>
{
	private readonly IPromotionChecker[] _checkers;

	public TagPlayerDepositCommandService(IPromotionCheckerFactory promotionCheckerFactory)
	{
		ArgumentNullException.ThrowIfNull(promotionCheckerFactory);

		var firstDepositChecker = promotionCheckerFactory.GetPromotionChecker("FirstDeposit");
		var AccountfirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("AccountFirstDeposit");
		var dailyFirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("DailyFirstDeposit");
		var depositRewardV2Checker = promotionCheckerFactory.GetPromotionChecker("DepositRewardV2");

		_checkers = [firstDepositChecker, AccountfirstDepositChecker, dailyFirstDepositChecker, depositRewardV2Checker];
	}

	public async ValueTask ExecuteAsync(TagPlayerDepositCommand command, CancellationToken cancellationToken = default)
	{
		foreach (var checker in _checkers)
			await checker
				.TagPromotionTypeAsync(command.PlayerCode, command.PromotionId, cancellationToken)
				.ConfigureAwait(false);
	}
}
