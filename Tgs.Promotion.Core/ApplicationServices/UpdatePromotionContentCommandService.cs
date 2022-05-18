using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionContentCommandService : ICommandService<UpdatePromotionContentCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public UpdatePromotionContentCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(UpdatePromotionContentCommand command, CancellationToken cancellationToken = default)
		=> _promotionRepository.SetPromotionContentAsync(
			command.PromotionId,
			new PromotionContentDetail(
				command.SubSubject,
				command.PersonalComputerBanner,
				command.MobileBanner,
				command.Content,
				command.Convention,
				command.Localization),
			command.Modifier,
			cancellationToken);
}
