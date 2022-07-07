using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CancelTagPlayerDepositCommandService : ICommandService<CancelTagPlayerDepositCommand>
{
	private readonly IPromotionChecker[] _checkers;

	public CancelTagPlayerDepositCommandService(IPromotionCheckerFactory promotionCheckerFactory)
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

	public async ValueTask ExecuteAsync(CancelTagPlayerDepositCommand command, CancellationToken cancellationToken = default)
	{
		foreach (var checker in _checkers)
			await checker
				.RollbackTagAsync(command.PlayerCode, command.PromotionId)
				.ConfigureAwait(false);
	}
}
