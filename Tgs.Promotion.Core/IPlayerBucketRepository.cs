using Tgs.Promotion.Core.Models.PromotionBucket;

namespace Tgs.Promotion.Core
{
	public interface IPlayerBucketRepository
	{
		ValueTask<CreatePlayerBucketWithDepositResponse> CreatePlayerBucketWithDepositAsync(
			CreatePlayerBucketWithDepositRequest request);

		ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] request);

		ValueTask<bool> IsBucketFullAsync(string playerCode);

		ValueTask<decimal> ClearPlayerBucketsAsync(string playerCode);
	}
}
