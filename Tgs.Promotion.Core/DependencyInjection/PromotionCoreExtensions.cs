using System.Collections.Immutable;
using System.Reflection;
using Tgs.Promotion;
using Tgs.Promotion.ApplicationServices.Promotion;
using Tgs.Promotion.ApplicationServices.Promotion.Workers;
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
			.RegisterPromotionChecker<DepositRewardChecker>("DepositReward");

		_ = services
			.AddSerialNumberGenerator()
			.AddSingleton<PromotionSerializer>()
			.AddScoped<IPromotionCheckerFactory>(sp => ActivatorUtilities.CreateInstance<PromotionCheckerFactory>(
				sp,
				configuration.GetAllRegisterCheckerTypes()));

		_ = services
			.AddTransient<IDistributionTaskQueueProcessor, DistributionTaskQueueProcessor>()
			.AddTransient<ReliefMoneyWeeklyDistribution>()
			.AddTransient<TurnoverRebateWeeklyDistribution>()
			.AddTransient<TurnoverRebateDailyDistribution>()
			.AddTransient<ReliefMoneyMonthlyDistribution>()
			.AddTransient<ReliefMoneyDailyDistribution>()
			.AddTransient<Func<string, IDistribution>>(serviceProvider => key =>
			{
				var distribution = new Dictionary<string, Type>
				{
					{ "ReliefMoneyWeekly", typeof(ReliefMoneyWeeklyDistribution) },
					{ "TurnoverRebateWeekly", typeof(TurnoverRebateWeeklyDistribution) },
					{ "TurnoverRebate", typeof(TurnoverRebateDailyDistribution) },
					{ "ReliefMoneyMonthly", typeof(ReliefMoneyMonthlyDistribution) },
					{ "ReliefMoney", typeof(ReliefMoneyDailyDistribution) }
				}.ToImmutableDictionary();

			_ = distribution.TryGetValue(key, out var distributionType);

			return (IDistribution)serviceProvider.GetService(distributionType!)!;
		});

		_ = services.AddSingleton<ISystemClock, DefaultSystemClock>();

		return services;
	}
}
