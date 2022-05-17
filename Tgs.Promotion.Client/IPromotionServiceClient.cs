namespace Tgs.Promotion.Client;

public interface IPromotionServiceClient
{
	ValueTask CreatePromotionAsync<TCampaignDetail>(
		Guid brandId,
		CreatePromotionInfo<TCampaignDetail> createInfo,
		string modifier,
		CancellationToken cancellationToken = default);
}
