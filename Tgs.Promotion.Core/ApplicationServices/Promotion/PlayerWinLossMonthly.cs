namespace Tgs.Promotion.ApplicationServices.Promotion;

public class PlayerWinLossMonthly
{
	public Guid BrandId { get; set; }

	public Guid GameProviderId { get; set; }

	public short GameType { get; set; }

	public string PlayerCode { get; set; } = default!;

	public int? VipLevel { get; set; }

	public decimal WinLoss { get; set; }
}
