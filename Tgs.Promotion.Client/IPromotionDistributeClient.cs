using Tgs.Promotion.Clients.Models;

namespace Tgs.Promotion.Clients;

public interface IPromotionDistributeClient
{
	Task<Guid?> DistributePromotionAsync(Guid brandId, Guid promotionId, int conditionHash, string playerCode,
		decimal amount, string? distributor);
}