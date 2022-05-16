using Tgs.Promotion.Client.Models;
using Tgs.Promotion.Client.Models.Promotion;

namespace Tgs.Promotion.Client
{
	public interface IPromotionClient
	{
		Task<MessageResponse> ChangeOrderAsync(ChangeOrderRequest request);

		Task<MessageResponse> ChangePromotionStatusAsync(UpdatePromotionStatusRequest request);

		Task<bool> CheckCanJoinDepositPromotionAsync(Guid brandId, string playerCode, Guid promotionId, decimal amount);

		Task<MessageResponse> CreatePromotionAsync(CreatePromotionRequest request);

		Task<MessageResponse> DepositJoinAsync(Guid promotionId, string playerCode);

		Task<PagedResponse<JoinedPromotionModel>> GetJoinedPromotionsAsync(GetJoinedPromotionsRequest request);

		Task<PromotionBonusOfDepositModel[]> GetPromotionBonusOfDepositAsync(int[] promotionPlayerIds);

		Task<PromotionNameModel[]> GetPromotionNamesAsync(GetPromotionNamesRequest request);

		Task<PagedResponse<PromotionModel>> GetPromotionsAsync(GetPromotionsRequest request);

		Task<PagedResponse<PromotionModel>> GetValidPromotionsAsync(GetValidPromotionsRequest request);

		Task<MessageResponse> JoinAsync(JoinPromotionRequest request);

		Task RevertPromotionAsync(Guid promotionId, decimal amount);

		Task TagPlayerDepositAsync(string playerCode, Guid? promotionId);

		Task<MessageResponse> UpdatePromotionAsync(UpdatePromotionRequest request);
	}
}
