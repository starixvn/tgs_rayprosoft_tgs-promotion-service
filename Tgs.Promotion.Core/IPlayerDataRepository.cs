namespace Tgs.Promotion.Core;

public interface IPlayerDataRepository
{
	ValueTask<bool> CheckPlayerFirstDepositingAsync(string playerCode);

	ValueTask<bool> CheckPlayerFirstJoinPromotionAsync(string playerCode);

	ValueTask<bool> CheckPlayerIsDailyFirstDepositAsync(string playerCode);

	ValueTask RemovePlayerDailyDepositFlagAsync(string playerCode);

	ValueTask RemovePlayerFirstDepositFlagAsync(string playerCode);

	ValueTask RemovePlayerFirstJoinPromotionAsync(string playerCode);

	ValueTask SetPlayerDailyDepositFlagAsync(string playerCode);

	ValueTask SetPlayerFirstDepositFlagAsync(string playerCode);

	ValueTask SetPlayerFirstJoinPromotionAsync(string playerCode);
}
