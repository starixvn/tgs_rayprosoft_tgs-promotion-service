namespace Tgs.Promotion.Domain.Models.PromotionDispatch
{
    public class PromotionDispatchModel
    {
        public decimal BonusAmount { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public string PlayerCode { get; set; } = default!;

        public Guid PromotionBonusId { get; set; }

        public string PromotionGroup { get; set; } = default!;

        public Guid PromotionId { get; set; }

        public string PromotionName { get; set; } = default!;

        public string PromotionType { get; set; } = default!;

        public DateTimeOffset? SentOn { get; set; }

        public string Username { get; set; } = default!;
    }
}
