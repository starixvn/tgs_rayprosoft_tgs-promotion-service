using System.Text.Json;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CreatePromotionMainInfoCommandService : IGetService<CreatePromotionMainInfoCommand, Guid>
{
	private readonly IPromotionRepository _promotionRepository;

	public CreatePromotionMainInfoCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<Guid> GetAsync(CreatePromotionMainInfoCommand command, CancellationToken cancellationToken = default)
	{
		var createInfo = new CreatePromotionMainInfo(
			command.BrandId,
			command.Name,
			command.EventDuring,
			command.PromotionType,
			command.PromotionLimitAmount,
			command.IsAutoSent);

		return _promotionRepository
			.CreatePromotionAsync(createInfo, command.Modifier, cancellationToken);
	}
}
