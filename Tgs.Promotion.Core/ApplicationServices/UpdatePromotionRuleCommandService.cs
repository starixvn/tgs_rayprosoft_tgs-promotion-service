using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionRuleCommandService : ICommandService<UpdatePromotionRuleCommand>
{
	private readonly IPromotionRepository _promotionRepository;

	public UpdatePromotionRuleCommandService(IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask ExecuteAsync(UpdatePromotionRuleCommand command, CancellationToken cancellationToken = default)
		=> _promotionRepository.SetPromotionRuleAsync(
			command.PromotionId,
			new PromotionRule(
				command.CampaignDetail,
				command.BonusAmountLimit,
				command.Percentage,
				command.TurnoverMultiple),
			command.Modifier,
			cancellationToken);
}
