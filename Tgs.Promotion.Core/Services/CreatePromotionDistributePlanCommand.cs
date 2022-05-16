namespace Tgs.Promotion.Core.Services
{
	public record CreatePromotionDistributePlanCommand(Guid PromotionId, decimal Amount);
}
