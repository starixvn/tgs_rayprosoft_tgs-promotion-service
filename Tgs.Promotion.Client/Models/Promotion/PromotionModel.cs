using System.Text.Json;

namespace Tgs.Promotion.Client.Models.Promotion
{
	public record PromotionModel
	{
		public decimal AmountUsed { get; set; }

		public bool AutoSentPlayerBonus { get; set; }

		public decimal BonusAmountLimit { get; set; }

		public Guid BrandId { get; set; }

		public string? Content { get; set; }

		public string? CreatedByUsername { get; set; }

		public DateTimeOffset CreatedOn { get; set; }

		public string? DesktopBanner { get; set; }

		public DateTimeOffset From { get; set; }

		public Guid[]? GameProviderIds { get; set; }

		public short? GameType { get; set; }

		public Guid Id { get; set; }

		public IDictionary<string, JsonDocument>? Localization { get; set; }

		public string? MobileBanner { get; set; }

		public string Name { get; set; } = default!;

		public decimal Percentage { get; set; }

		public string PromotionType { get; set; } = default!;

		public short Status { get; set; }

		public string? SubSubject { get; set; }

		public string? Terms { get; set; }

		public DateTimeOffset To { get; set; }

		public decimal TotalAmountLimit { get; set; }

		public decimal TurnoverMultiple { get; set; }

		public bool Unlimited { get; set; }

		public string? UpdatedByUsername { get; set; }

		public DateTimeOffset UpdatedOn { get; set; }

		public int[] VipLevels { get; set; } = default!;

		public int Number { get; set; }
	}
}
