namespace Tgs.Promotion.Service.ViewModels;

public class DispatchPromotionBonusViewModel
{
	public Guid OperatorUserId { get; set; }

	public DispatchPlayerViewModel[] Players { get; set; } = default!;
}
