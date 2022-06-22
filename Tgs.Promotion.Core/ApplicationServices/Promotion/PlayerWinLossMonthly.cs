namespace Tgs.Promotion.ApplicationServices.Promotion;

public class PlayerWinLossMonthly
{
	public Guid BrandId { get; set; }
	public string PlayerCode { get; set; } = default!;
	public short GameType { get; set; }
	public int? VipLevel { get; set; }
	public Guid GameProviderId { get; set; }
	public decimal WinLoss { get; set; }
	public DateTimeOffset ReportDate { get; set; }
}