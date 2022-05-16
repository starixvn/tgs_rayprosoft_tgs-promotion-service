using Tgs.Promotion.Domain.Models.PromotionBucket;

namespace Tgs.Promotion.Domain.Interfaces
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
