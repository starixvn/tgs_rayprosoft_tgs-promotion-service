using System.Collections.Immutable;
using System.Reflection;
using Tgs.Promotion;
using Tgs.Promotion.Checkers;
using Tgs.Promotion.Configuration;

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
			.RegisterPromotionChecker<AlwaysTrueChecker>("TurnoverRebateWeekly")
			.RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyDaily")
			.RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyWeekly")
			.RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyMonthly")
			.RegisterPromotionChecker<RegisterChecker>("Register")
			.RegisterPromotionChecker<AlwaysTrueChecker>("Roulette")
			.RegisterPromotionChecker<AlwaysTrueChecker>("Dummy")
			.RegisterPromotionChecker<AlwaysTrueChecker>("DepositAccumulateRoulette")
			.RegisterPromotionChecker<DepositRewardChecker>("DepositReward")
			.RegisterPromotionChecker<AlwaysTrueChecker>("PromoteDepositChannel");

		_ = services
			.AddSerialNumberGenerator()
			.AddSingleton<PromotionSerializer>()
			.AddScoped<IPromotionCheckerFactory>(sp => ActivatorUtilities.CreateInstance<PromotionCheckerFactory>(
				sp,
				configuration.GetAllRegisterCheckerTypes()));

		_ = services.AddSingleton<ISystemClock, DefaultSystemClock>();

		return services;
	}
}
