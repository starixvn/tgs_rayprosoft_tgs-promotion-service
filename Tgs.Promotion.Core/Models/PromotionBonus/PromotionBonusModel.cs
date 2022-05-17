using Tgs.Promotion.Models.PromotionBonus;
namespace Tgs.Promotion.Models.PromotionBonus;

public class PromotionBonusModel
{
	public bool AutoSentPlayerBonus { get; set; }

	public decimal BonusAmountLimit { get; set; }

	public Guid BrandId { get; set; }

	public DateTimeOffset CreatedOn { get; set; }

	public DateTimeOffset From { get; set; }

	public GameType? GameType { get; set; }

	public Guid Id { get; set; }

	public decimal Percentage { get; set; }

	public PromotionBonusPlayerModel[] Players { get; set; } = default!;

	public Guid PromotionId { get; set; }

	public string PromotionType { get; set; } = default!;

	public DateTimeOffset? SentOn { get; set; }

	public DateTimeOffset To { get; set; }

	public decimal TotalAmountLimit { get; set; }

	public decimal TurnoverMultiple { get; set; }
}
