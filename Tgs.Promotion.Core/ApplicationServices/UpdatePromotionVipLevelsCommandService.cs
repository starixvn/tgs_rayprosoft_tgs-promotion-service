using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionVipLevelsCommandService : ICommandService<UpdatePromotionVipLevelsCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public UpdatePromotionVipLevelsCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(
		UpdatePromotionVipLevelsCommand command,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.SetPromotionVipLevelsAsync(
			command.PromotionId,
			command.VipLevels,
			command.Modifier,
			cancellationToken);
}
