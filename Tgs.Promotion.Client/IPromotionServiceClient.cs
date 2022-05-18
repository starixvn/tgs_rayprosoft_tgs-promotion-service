namespace Tgs.Promotion.Client;

public interface IPromotionServiceClient
{
	ValueTask<Guid> CreatePromotionAsync<TCampaignDetail>(
		Guid brandId,
		CreatePromotionMainInfo<TCampaignDetail> createInfo,
		string modifier,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionVipLevelsAsync(
		Guid promotionId,
		IEnumerable<int> vipLevels,
		string modifier,
		CancellationToken cancellationToken = default);
}
