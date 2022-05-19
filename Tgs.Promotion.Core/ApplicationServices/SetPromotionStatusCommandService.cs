using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class SetPromotionStatusCommandService : ICommandService<SetPromotionStatusCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public SetPromotionStatusCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(SetPromotionStatusCommand command, CancellationToken cancellationToken = default)
		=> _promotionRepository.SetPromotionStatusAsync(
			command.PromotionId,
			command.Status,
			command.Modifier,
			cancellationToken);
}
