namespace Tgs.Promotion.ApplicationServices;

public record GetEventDetailQuery(
	Guid PromotionId,
	Guid EventId);