namespace Tgs.Promotion.Service.ViewModels;

public record struct OrderViewModel(
	Guid PromotionId,
	int Order);
