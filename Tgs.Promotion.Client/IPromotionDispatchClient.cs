using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.PromotionDispatch;

namespace Tgs.Promotion.Clients;

public interface IPromotionDispatchClient
{
	Task<MessageResponse> DispatchPromotionAsync(DispatchPromotionBonusRequest request);

	Task<PagedResponse<PromotionDispatchModel>> GetPromotionDispatchesAsync(GetPromotionDispatchesRequest request);
}