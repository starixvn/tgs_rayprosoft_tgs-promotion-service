using System.Reflection;
using Tgs.Promotion;
using Tgs.Promotion.Checkers;
using Tgs.Promotion.Configuration;
using Tgs.Promotion.PromotionDetailMessages;

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
			.RegisterPromotionChecker<ExternalServiceChecker>("AccountFirstDeposit")
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
			.RegisterPromotionChecker<AlwaysTrueChecker>("PromoteDepositChannel")
			.RegisterPromotionChecker<ExternalServiceChecker>("DepositRewardV2")
			.RegisterPromotionChecker<ExternalServiceChecker>("Milestone");

		_ = configuration.RegisterPromotionDetail<DetailMessageReply>("Milestone")
			.RegisterPromotionDetail<DetailMessageReply>("FirstDeposit")
			.RegisterPromotionDetail<DetailMessageReply>("DailyFirstDeposit")
			.RegisterPromotionDetail<DetailMessageReply>("DailySecondDeposit");

		_ = services
			.AddSerialNumberGenerator()
			.AddSingleton<PromotionSerializer>()
			.AddScoped<IPromotionCheckerFactory>(sp => ActivatorUtilities.CreateInstance<PromotionCheckerFactory>(
				sp,
				configuration.GetAllRegisterCheckerTypes()))
			.AddScoped<IPromotionDetailFactory>(sp => ActivatorUtilities.CreateInstance<PromotionDetailFactory>(
				sp,
				configuration.GetAllRegisterDetailTypes()));

		_ = services.AddSingleton<ISystemClock, DefaultSystemClock>();

		return services;
	}
}
