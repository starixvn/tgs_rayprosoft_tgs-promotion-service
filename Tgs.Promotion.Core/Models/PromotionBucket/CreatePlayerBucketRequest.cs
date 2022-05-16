using Tgs.Promotion.Domain;

namespace Tgs.Promotion.Core.Models.PromotionBucket;

public record CreatePlayerBucketRequest(
	Guid BrandId,
	string PlayerCode,
	Guid DefaultWalletId,
	BucketType BucketType,
	decimal BucketAmount);
