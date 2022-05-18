using Microsoft.EntityFrameworkCore;
using Tgs.Promotion;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Repository;

namespace Microsoft.Extensions.DependencyInjection;

public static class TgsRepositoryExtensions
{
	public static IServiceCollection AddTgsRepository(this IServiceCollection services, string connectionString)
		=> services
			.AddDbContext<TgsContext>(options => options
				.UseNpgsql(connectionString)
				.UseLazyLoadingProxies())
			.AddTransient(typeof(ITgsService<>), typeof(TgsService<>))
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

	public static IServiceCollection AddRedis(
		this IServiceCollection service,
		Action<RedisOptions, IServiceProvider> configure)
		=> service
			.AddOptions<RedisOptions>()
			.Configure(configure)
			.Services
			.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();
}
