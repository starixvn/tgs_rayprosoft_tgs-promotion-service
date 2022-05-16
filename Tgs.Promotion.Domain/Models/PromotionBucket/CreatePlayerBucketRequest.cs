using Tgs.Promotion.Domain.shared;

namespace Tgs.Promotion.Domain.Models.PromotionBucket
{
	public record CreatePlayerBucketRequest(
		Guid BrandId,
		string PlayerCode,
		Guid DefaultWalletId,
		BucketType BucketType,
		decimal BucketAmount);
}
