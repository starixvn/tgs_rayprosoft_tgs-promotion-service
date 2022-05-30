using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetOrCreatePromotionDistributePlanCommandService
	: IGetService<GetOrCreatePromotionDistributePlanCommand, PromotionDistributePlanInfo>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetOrCreatePromotionDistributePlanCommandService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public async ValueTask<PromotionDistributePlanInfo> GetAsync(
		GetOrCreatePromotionDistributePlanCommand command,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _promotionRepository
			.GetPromotionByIdAsync(command.PromotionId)
			.ConfigureAwait(false);

		return promotion is null
			? throw new InvalidOperationException($"Can't find promotion ({command.PromotionId})")
			: await _promotionRepository
			.CreatePromotionDistributePlanAsync(
				promotion.Id,
				command.ConditionHash,
				cancellationToken)
			.ConfigureAwait(false);
	}
}
