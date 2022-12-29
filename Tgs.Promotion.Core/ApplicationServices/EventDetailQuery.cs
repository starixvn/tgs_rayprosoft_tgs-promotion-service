namespace Tgs.Promotion.ApplicationServices;

public record EventDetailQuery(
	Guid PromotionId,
	Guid EventId);