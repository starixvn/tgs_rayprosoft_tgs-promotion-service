using System.Reflection;
using Tgs.Promotion.Configuration;
using Tgs.Promotion;

namespace Microsoft.Extensions.DependencyInjection;

public static class PromotionCoreExtensions
{
	public static IServiceCollection AddPromotionCore(this IServiceCollection services)
	{
		_ = services.RegisterAllServiceUtilityTypes(
			Assembly.GetExecutingAssembly(),
			ServiceLifetime.Transient);

		var configuration = new PromotionConfiguration(services);

		_ = configuration
			.RegisterPromotionChecker<FirstDepositChecker>("AccountFirstDeposit")
			.RegisterPromotionChecker<FirstJoinPromotionChecker>("FirstDeposit")
			.RegisterPromotionChecker<DailyFirstDepositChecker>("DailyFirstDeposit")
			.RegisterPromotionChecker<DailySecondDepositChecker>("DailySecondDeposit")
			.RegisterPromotionChecker<AlwaysTrueChecker>("TurnoverRebate")
			.RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyDaily")
			.RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyWeekly")
			.RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyMonthly")
			.RegisterPromotionChecker<RegisterChecker>("Register");

		_ = services
			.AddSerialNumberGenerator()
			.AddScoped<IPromotionCheckerFactory>(sp => ActivatorUtilities.CreateInstance<PromotionCheckerFactory>(
				sp,
				configuration.GetAllRegisterCheckerTypes()));

		return services;
	}
}
