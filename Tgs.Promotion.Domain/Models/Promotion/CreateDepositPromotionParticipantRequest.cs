namespace Tgs.Promotion.Domain.Models.Promotion
{
    public record CreateDepositPromotionParticipantRequest
        (Guid PromotionId, string PlayerCode);
}