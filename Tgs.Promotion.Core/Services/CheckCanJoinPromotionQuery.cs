namespace Tgs.Promotion.Core.Services
{
    public record CheckCanJoinPromotionQuery(
        Guid BrandId,
        string PlayerCode,
        Guid PromotionId,
        decimal Amount);
}