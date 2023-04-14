using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.Promotion;

namespace Tgs.Promotion.Clients;

public interface IPromotionClient
{
	Task<bool> CheckCanJoinDepositPromotionAsync(Guid brandId, string playerCode, Guid promotionId, decimal amount);

	Task<bool> CheckPromotionJoinedAsync(string playerCode, Guid promotionId);

	Task<MessageResponse> DepositJoinAsync(Guid promotionId, string playerCode);

	Task<PagedResponse<JoinedPromotionModel>> GetJoinedPromotionsAsync(GetJoinedPromotionsRequest request);

	Task<PromotionBonusOfDepositModel[]> GetPromotionBonusOfDepositAsync(int[] promotionPlayerIds);

	Task<PromotionNameModel[]> GetPromotionNamesAsync(GetPromotionNamesRequest request);

	[Obsolete("過時")]
	Task<PagedResponse<PromotionModel>> GetPromotionsAsync(GetPromotionsRequest request);

	Task<PagedResponse<PromotionModel>> GetValidPromotionsAsync(GetValidPromotionsRequest request);

	Task<MessageResponse> JoinAsync(JoinPromotionRequest request);

	Task RevertPromotionAsync(Guid promotionId, decimal amount);

	Task TagPlayerDepositAsync(string playerCode, Guid? promotionId);

	Task UntagPlayerDepositAsync(string playerCode, Guid? promotionId);
}
