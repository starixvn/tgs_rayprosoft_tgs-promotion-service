using Microsoft.Extensions.DependencyInjection;
using Tgs.CashService.Client;

namespace Tgs.Promotion.Configuration;

public class PromotionConfiguration(IServiceCollection services)
{
	private readonly Dictionary<string, Type> _checkerTypes = [];

	public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));

	public PromotionConfiguration AddCashServiceClient(
		Action<CashServiceClientOptions, IServiceProvider> configure)
	{
		_ = Services.AddCashServiceClient(configure);

		return this;
	}

	public PromotionConfiguration AddPaymentServiceClient(
		Action<IHttpClientBuilder> configure)
	{
		_ = Services.AddPaymentServiceClient(configure);

		return this;
	}

	public PromotionConfiguration AddPlayerDataRepository<TPlayerDataRepository>()
		where TPlayerDataRepository : class, IPlayerDataRepository
	{
		_ = Services.AddTransient<IPlayerDataRepository, TPlayerDataRepository>();

		return this;
	}

	public PromotionConfiguration AddPromotionBonusRepository<TPromotionBonusRepository>()
		where TPromotionBonusRepository : class, IPromotionBonusRepository
	{
		_ = Services.AddTransient<IPromotionBonusRepository, TPromotionBonusRepository>();

		return this;
	}

	public PromotionConfiguration AddPromotionParticipantRepository<TPromotionParticipantRepository>()
		where TPromotionParticipantRepository : class, IPromotionParticipantRepository
	{
		_ = Services.AddTransient<IPromotionParticipantRepository, TPromotionParticipantRepository>();

		return this;
	}

	public PromotionConfiguration AddPromotionPlayerRepository<TPromotionPlayerRepository>()
		where TPromotionPlayerRepository : class, IPromotionPlayerRepository
	{
		_ = Services.AddTransient<IPromotionPlayerRepository, TPromotionPlayerRepository>();

		return this;
	}

	public PromotionConfiguration AddPromotionRepository<TPromotionRepository>()
		where TPromotionRepository : class, IPromotionRepository
	{
		_ = Services.AddTransient<IPromotionRepository, TPromotionRepository>();

		return this;
	}

	public PromotionConfiguration AddSettingRepository<TSettingRepository>()
		where TSettingRepository : class, ISettingsRepository
	{
		_ = Services.AddTransient<ISettingsRepository, TSettingRepository>();

		return this;
	}

	public PromotionConfiguration RegisterPromotionChecker<TChecker>(string typeName)
		where TChecker : class, IPromotionChecker
	{
		_checkerTypes[typeName] = typeof(TChecker);

		return this;
	}

	internal IDictionary<string, Type> GetAllRegisterCheckerTypes() => _checkerTypes;
}
