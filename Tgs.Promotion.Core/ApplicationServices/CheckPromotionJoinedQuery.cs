namespace Tgs.Promotion.ApplicationServices;
public record CheckPromotionJoinedQuery(
	string PlayerCode,
	Guid PromotionId);
