using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.PromotionBonus;

namespace Tgs.Promotion.Clients;

public interface IPromotionBonusClient
{
	[Obsolete("過時")]
	Task<MessageResponse> CreatePromotionSettleDataDailyAsync(CreatePromotionBonusDailyRequest request);

	[Obsolete("過時")]
	Task<MessageResponse> CreatePromotionSettleDataMonthlyAsync(CreatePromotionBonusMonthlyRequest request);

	[Obsolete("過時")]
	Task<MessageResponse> CreatePromotionSettleDataWeeklyAsync(CreatePromotionBonusWeeklyRequest request);
}
