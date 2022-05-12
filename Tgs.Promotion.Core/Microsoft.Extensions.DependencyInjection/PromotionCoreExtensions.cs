using System.Reflection;
using MediatR;
using Tgs.Promotion.Core;
using Tgs.Promotion.Core.Configuration;
using Tgs.Promotion.Domain.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class PromotionCoreExtensions
{
	public static IServiceCollection AddPromotionCore(this IServiceCollection services)
	{
        services
            .RegisterAllServiceUtilityTypes(
                Assembly.GetExecutingAssembly(),
                ServiceLifetime.Transient)
            .AddMediatR(Assembly.GetExecutingAssembly());

        var configuration = new PromotionConfiguration(services);

        configuration
            .RegisterPromotionChecker<FirstDepositChecker>("AccountFirstDeposit")
            .RegisterPromotionChecker<FirstJoinPromotionChecker>("FirstDeposit")
            .RegisterPromotionChecker<DailyFirstDepositChecker>("DailyFirstDeposit")
            .RegisterPromotionChecker<DailySecondDepositChecker>("DailySecondDeposit")
            .RegisterPromotionChecker<AlwaysTrueChecker>("TurnoverRebate")
            .RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyDaily")
            .RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyWeekly")
            .RegisterPromotionChecker<AlwaysTrueChecker>("ReliefMoneyMonthly");

        services
            .AddScoped<IPromotionCheckerFactory>(
                sp => ActivatorUtilities.CreateInstance<PromotionCheckerFactory>(
                    sp,
                    configuration.GetAllRegisterCheckerTypes()))
            .AddTransient<IPlayerRepository, PlayerRepository>()
            .AddTransient<ISettingsRepository, SettingsRepository>()
            .AddTransient<IPromotionTypeRepository, PromotionTypeRepository>()
            .AddTransient<IPromotionJoinLockRepository, PromotionJoinLockRepository>()
            .AddTransient<ITransactionRepository, TransactionRepository>()
            .AddTransient<IPromotionRepository, PromotionRepository>()
            .AddTransient<IPromotionBonusRepository, PromotionBonusRepository>()
            .AddTransient<IPromotionParticipantRepository, PromotionParticipantRepository>()
            .AddTransient<ISettingsRepository, SettingsRepository>()
            .AddTransient<IPromotionPlayerRepository, PromotionPlayerRepository>()
            .AddTransient<IPlayerBucketRepository, PlayerBucketRepository>()
            .AddTransient<IPlayerDataRepository, RedisPlayerDataRepository>();

        return services;
	}
}
