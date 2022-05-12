namespace Tgs.Promotion.Core.Services
{
    public record RevertPromotionAmountCommand(Guid PromotionId, decimal Amount);
}
