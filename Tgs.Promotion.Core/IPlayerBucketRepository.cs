using Tgs.Promotion.Models.PromotionBucket;

namespace Tgs.Promotion;

public interface IPlayerBucketRepository
{
	ValueTask CreatePlayerBucketAsync(
		string playerCode,
		BucketType bucketType,
		decimal bucketAmount,
		CancellationToken cancellationToken = default);

	ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] request);
}
