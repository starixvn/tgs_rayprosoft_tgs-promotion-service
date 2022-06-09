using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionRuleCommandService : ICommandService<UpdatePromotionRuleCommand>
{
	private readonly IPromotionRepository _promotionRepository;
	private readonly PromotionSerializer _promotionSerializer;

	public UpdatePromotionRuleCommandService(IPromotionRepository promotionRepository, PromotionSerializer promotionSerializer)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));
	}

	public async ValueTask ExecuteAsync(UpdatePromotionRuleCommand command, CancellationToken cancellationToken = default)
	{
		if (command.GameType is not null)
		{
			var settings = _promotionSerializer.Deserialize<GameProviderSettings>(command.CampaignDetail);

			if (settings is not null)
				await _promotionRepository
					.SetPromotionGameProvidersAsync(command.PromotionId, settings.GameProviderIds, cancellationToken)
					.ConfigureAwait(false);
		}

		await _promotionRepository.SetPromotionRuleAsync(
			command.PromotionId,
			new PromotionRule(
				command.CampaignDetail,
				command.BonusAmountLimit,
				command.Percentage,
				command.TurnoverMultiple,
				command.GameType),
			command.Modifier,
			cancellationToken).ConfigureAwait(false);
	}
}
