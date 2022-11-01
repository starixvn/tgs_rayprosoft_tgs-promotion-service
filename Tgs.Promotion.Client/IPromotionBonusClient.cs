using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.PromotionBonus;

namespace Tgs.Promotion.Clients;

public interface IPromotionBonusClient
{
	Task<MessageResponse> CreatePromotionSettleDataDailyAsync(CreatePromotionBonusDailyRequest request);

	Task<MessageResponse> CreatePromotionSettleDataMonthlyAsync(CreatePromotionBonusMonthlyRequest request);

	Task<MessageResponse> CreatePromotionSettleDataWeeklyAsync(CreatePromotionBonusWeeklyRequest request);
}