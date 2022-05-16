using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.Promotion.Core.Extensions;
using Tgs.Promotion.Domain;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Models.PromotionBucket;

namespace Tgs.Promotion.Core
{
	public class PlayerBucketRepository : IPlayerBucketRepository
	{
		private readonly ICashServiceClient _cashServiceClient;
		private readonly ITgsService<PlayerBucket> _playerBucketService;
		private readonly ITgsService<PromotionBonus> _promotionBonusService;
		private readonly ITgsService<Domain.Entities.Promotion> _promotionService;
		private readonly ITgsService<TransactionPromotion> _transactionPromotionService;

		public PlayerBucketRepository(
			ITgsService<PlayerBucket> playerBucketService,
			ITgsService<Domain.Entities.Promotion> promotionService,
			ITgsService<PromotionBonus> promotionBonusService,
			ITgsService<TransactionPromotion> transactionPromotionService,
			ICashServiceClient cashServiceClient)
		{
			_playerBucketService = playerBucketService ?? throw new ArgumentNullException(nameof(playerBucketService));
			_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
			_promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
			_promotionBonusService =
				promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
			_transactionPromotionService = transactionPromotionService ??
				throw new ArgumentNullException(nameof(transactionPromotionService));
		}

		public async ValueTask<decimal> ClearPlayerBucketsAsync(string playerCode)
		{
			await _playerBucketService.GetQueries().Where(x => x.PlayerCode == playerCode)
				.ForEachAsync(x =>
				{
					x.CurrentBucketAmount = x.BucketAmount;
					x.IsFull = true;
				}).ConfigureAwait(false);
			await _playerBucketService.SaveChangesAsync().ConfigureAwait(false);

			return await _playerBucketService.GetQueries().Where(x => x.PlayerCode == playerCode && x.IsFull)
				.SumAsync(x => x.BucketAmount - x.CurrentBucketAmount);
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

			await _playerBucketService.CreateEntitiesAsync(buckets).ConfigureAwait(false);
		}

		public async ValueTask<CreatePlayerBucketWithDepositResponse> CreatePlayerBucketWithDepositAsync(
			CreatePlayerBucketWithDepositRequest request)
		{
			var depositBonusAmount = request.DepositAmount;
			var promotionPlayerId = default(int?);

			await _cashServiceClient.DepositAsync(
				request.DefaultWalletId,
				request.DepositAmount,
				request.Remark,
				request.FormId.ToString(),
				request.UserId).ConfigureAwait(false);

			if (request.PromotionId != null)
			{
				var promotion = await _promotionService.GetQueries()
					.FirstOrDefaultAsync(x => x.Id == request.PromotionId.Value);

				var bonusAmount = promotion!.Percentage * request.DepositAmount / 100M;

				bonusAmount = bonusAmount > promotion.BonusAmountLimit
					? promotion.BonusAmountLimit
					: bonusAmount;

				depositBonusAmount += bonusAmount;

				var playerBucket = new PlayerBucket
				{
					PlayerCode = request.PlayerCode,
					BucketAmount = depositBonusAmount * promotion.TurnoverMultiple,
					BucketType = (short)BucketType.Promotion,
					IsFull = false,
					CreatedOn = DateTime.UtcNow
				};

				var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));

				var reportDate = new DateTimeOffset(now.Year,
					now.Month,
					now.Day,
					0,
					0,
					0,
					now.Offset);

				var bonusId = GuidExtensions.NewSequentialGuid();

				var bonus = new PromotionBonus
				{
					Id = bonusId,
					BrandId = request.BrandId,
					PromotionId = promotion.Id,
					From = reportDate.UtcDateTime,
					To = reportDate.UtcDateTime,
					PromotionType = promotion.PromotionType,
					TotalAmountLimit = promotion.TotalAmountLimit,
					GameType = promotion.GameType,
					Percentage = promotion.Percentage,
					BonusAmountLimit = promotion.BonusAmountLimit,
					TurnoverMultiple = promotion.TurnoverMultiple,
					CreatedOn = DateTimeOffset.UtcNow.UtcDateTime,
					PromotionPlayers = new[]
					{
						new PromotionPlayer
						{
							PlayerCode = request.PlayerCode,
							PromotionBonusId = bonusId,
							BonusAmount = bonusAmount,
							SentOn = promotion.AutoSentPlayerBonus ? DateTimeOffset.UtcNow.UtcDateTime : null
						}
					}.ToHashSet()
				};

				await _promotionBonusService.CreateEntityAsync(bonus).ConfigureAwait(false);

				await _playerBucketService.CreateEntityAsync(playerBucket).ConfigureAwait(false);

				var response = await _cashServiceClient.PromotionAsync(
					request.DefaultWalletId,
					bonusAmount,
					null,
					null,
					null).ConfigureAwait(false);

				var transactionPromotion = new TransactionPromotion
				{
					TransactionId = response.TransactionId,
					PromotionId = promotion.Id,
					PromotionBonusId = bonusId,
					PromotionType = promotion.PromotionType
				};

				await _transactionPromotionService.CreateEntityAsync(transactionPromotion).ConfigureAwait(false);

				promotionPlayerId = bonus.PromotionPlayers.First().Id;
			}
			else
			{
				var bucket = new PlayerBucket
				{
					PlayerCode = request.PlayerCode,
					BucketAmount = request.DepositAmount,
					BucketType = (short)BucketType.Deposit,
					IsFull = false,
					CreatedOn = DateTimeOffset.UtcNow.UtcDateTime
				};

				await _playerBucketService.CreateEntityAsync(bucket).ConfigureAwait(false);
			}

			return new CreatePlayerBucketWithDepositResponse(depositBonusAmount, promotionPlayerId);
		}

		public async ValueTask<bool> IsBucketFullAsync(string playerCode) =>
			await _playerBucketService.GetQueries().Where(x => x.PlayerCode == playerCode).AllAsync(x => x.IsFull);
	}
}
