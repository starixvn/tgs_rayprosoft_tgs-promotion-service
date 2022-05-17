namespace Tgs.Promotion.Models.PromotionBucket;

public record CreatePlayerBucketRequest(
	Guid BrandId,
	string PlayerCode,
	Guid DefaultWalletId,
	BucketType BucketType,
	decimal BucketAmount);
