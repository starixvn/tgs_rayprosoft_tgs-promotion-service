using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models.PromotionBucket;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.Repository;

internal class PlayerBucketRepository : IPlayerBucketRepository
{
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly TgsContext _tgsContext;

	public PlayerBucketRepository(
		TgsContext tgsContext,
		ISerialNumberGenerator serialNumberGenerator)
	{
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async ValueTask CreatePlayerBucketAsync(
		string playerCode,
		BucketType bucketType,
		decimal bucketAmount,
		CancellationToken cancellationToken = default)
	{
		var player = await _tgsContext.Players
			.FindAsync(new object?[] { playerCode }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			return;

		var newId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var playerBucket = new PlayerBucket
		{
			Id = newId,
			PlayerCode = playerCode,
			CreatedOn = DateTime.UtcNow,
			BucketType = (short)bucketType,
			BucketAmount = bucketAmount,
			IsFull = false,
			CurrentBucketAmount = 0M
		};

		_ = await _tgsContext.PlayerBuckets
			.AddAsync(playerBucket, cancellationToken)
			.ConfigureAwait(false);

		_ = await _tgsContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests)
	{
		var buckets = requests.Select(x => new PlayerBucket
		{
			PlayerCode = x.PlayerCode,
			BucketAmount = x.BucketAmount,
			BucketType = (short)x.BucketType,
			IsFull = false,
			CreatedOn = DateTime.UtcNow
		}).ToArray();

		await _tgsContext.AddRangeAsync(buckets).ConfigureAwait(false);
	}
}
