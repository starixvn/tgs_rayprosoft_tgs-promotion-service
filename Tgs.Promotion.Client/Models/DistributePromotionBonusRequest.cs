namespace Tgs.Promotion.Clients.Models;

public class DistributePromotionBonusRequest
{
	public string Distributor { get; set; } = default!;

	public DistributePlayer[] Players { get; set; } = default!;
}

public class DistributePlayer
{
	public string PlayerCode { get; set; } = default!;

	public Guid PromotionBonusId { get; set; }
}
