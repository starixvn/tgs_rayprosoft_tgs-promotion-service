namespace Tgs.Promotion.ApplicationServices.Promotion;

public class PlayerTurnoverWeekly
{
	public Guid BrandId { get; set; }

	public Guid GameProviderId { get; set; }

	public short GameType { get; set; }

	public string PlayerCode { get; set; } = default!;

	public DateTimeOffset ReportDate { get; set; }

	public decimal Turnover { get; set; }

	public int? VipLevel { get; set; }
}
