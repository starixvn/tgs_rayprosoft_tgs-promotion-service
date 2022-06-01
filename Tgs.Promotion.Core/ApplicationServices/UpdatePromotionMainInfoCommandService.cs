using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionMainInfoCommandService : ICommandService<UpdatePromotionMainInfoCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public UpdatePromotionMainInfoCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(UpdatePromotionMainInfoCommand command, CancellationToken cancellationToken = default)
		=> _promotionRepository.UpdatePromotionMainInfoAsync(
			command.PromotionId,
			new UpdatePromotionMainInfo(
				command.Name,
				command.EventDuring,
				command.PromotionType,
				command.PromotionLimitAmount,
				command.IsAutoSent),
			command.Modifier,
			cancellationToken);
}
