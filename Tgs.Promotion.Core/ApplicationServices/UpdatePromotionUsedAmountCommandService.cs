using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionUsedAmountCommandService : ICommandService<UpdatePromotionUsedAmountCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public UpdatePromotionUsedAmountCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(UpdatePromotionUsedAmountCommand command, CancellationToken cancellationToken = default)
		=> _promotionRepository.UpdatePromotionUsedAmountAsync(
			command.PromotionId,
			command.TotalAmount,
			command.SnapshotId,
			cancellationToken);
}
