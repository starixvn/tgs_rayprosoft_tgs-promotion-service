using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CreatePromotionCommandService : ICommandService<CreatePromotionCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public CreatePromotionCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public async ValueTask ExecuteAsync(
		CreatePromotionCommand command,
		CancellationToken cancellationToken = default)
	{
		var createInfo = new CreatePromotionInfo
		{
			BrandId = command.BrandId,
			Name = command.Name,
			EventDuring = command.EventDuring,
			PromotionLimitAmount = command.PromotionLimitAmount,
			IsAutoSent = command.IsAutoSent,
			PromotionType = command.PromotionType,
			CampaignDetail = command.CampaignDetail
		};

		_ = await _promotionRepository
			.CreatePromotionAsync(createInfo, cancellationToken)
			.ConfigureAwait(false);
	}
}
