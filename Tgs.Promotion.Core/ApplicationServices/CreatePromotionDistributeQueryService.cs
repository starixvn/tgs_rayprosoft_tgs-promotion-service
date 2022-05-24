using System.Text.Json;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CreatePromotionDistributeQueryService : IGetService<CreatePromotionDistributeQuery, Guid?>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerRepository _playerRepository;
	private readonly IPromotionDistributePlanRepository _promotionDistributePlanRepository;
	private readonly IPromotionDistributeRepository _promotionDistributeRepository;
	private readonly IPromotionRepository _promotionRepository;

	public CreatePromotionDistributeQueryService(IPromotionDistributePlanRepository promotionDistributePlanRepository,
		IPromotionRepository promotionRepository, IPromotionDistributeRepository promotionDistributeRepository,
		ICashServiceClient cashServiceClient, IPlayerRepository playerRepository)
	{
		_promotionDistributePlanRepository = promotionDistributePlanRepository ??
			throw new ArgumentNullException(nameof(promotionDistributePlanRepository));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionDistributeRepository = promotionDistributeRepository ??
			throw new ArgumentNullException(nameof(promotionDistributeRepository));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
	}

	public async ValueTask<Guid?> GetAsync(CreatePromotionDistributeQuery command,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _promotionRepository.GetPromotionByIdAsync(command.PromotionId).ConfigureAwait(false);

		if (promotion is null)
		{
			return null;
		}

		var player = await _playerRepository.GetPlayerAsync(x => x.PlayerCode == command.PlayerCode)
			.ConfigureAwait(false);

		if (player is null)
		{
			return null;
		}

		var distributeId = await _promotionDistributeRepository.CreatePromotionDistributeAsync(
			promotion.BrandId,
			promotion.Id, 
			command.PlanId,
			command.Amount, 
			promotion.PromotionType, 
			command.PlayerCode, 
			command.Distributor);

		_ = _cashServiceClient.PromotionAsync(player.DefaultWalletId, command.Amount, null, distributeId.ToString(),
			null, cancellationToken);

		return distributeId;
	}
}