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
		var createInfo = new CreatePromotionMainInfo
		{
			BrandId = command.BrandId,
			Name = command.Name,
			EventDuring = command.EventDuring,
			PromotionLimitAmount = command.PromotionLimitAmount,
			IsAutoSent = command.IsAutoSent,
			PromotionType = command.PromotionType
		};

		return _promotionRepository
			.CreatePromotionAsync(createInfo, cancellationToken);
	}
}
