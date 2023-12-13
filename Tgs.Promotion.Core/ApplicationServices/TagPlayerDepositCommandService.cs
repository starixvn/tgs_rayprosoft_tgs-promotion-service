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

		_checkers = [firstDepositChecker, AccountfirstDepositChecker, dailyFirstDepositChecker];
	}

	public async ValueTask ExecuteAsync(TagPlayerDepositCommand command, CancellationToken cancellationToken = default)
	{
		foreach (var checker in _checkers)
			await checker
				.TagPromotionTypeAsync(command.PlayerCode, command.PromotionId)
				.ConfigureAwait(false);
	}
}
