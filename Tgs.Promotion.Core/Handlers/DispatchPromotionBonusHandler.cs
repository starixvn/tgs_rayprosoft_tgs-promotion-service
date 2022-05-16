using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.PromotionBucket;
using Tgs.Promotion.Domain.Models.PromotionDispatch;
using Tgs.Promotion.Domain.shared;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Handlers
{
	public class DispatchPromotionBonusHandler : IGetService<DispatchPromotionBonusRequest, MessageResponse>
	{
		private readonly ICashServiceClient _cashServiceClient;
		private readonly IPromotionBonusRepository _promotionBonusService;
		private readonly IPlayerBucketRepository _service;
		private readonly ITransactionRepository _transactionRepository;

		public DispatchPromotionBonusHandler(
			IPlayerBucketRepository service,
			IPromotionBonusRepository promotionBonusService,
			ICashServiceClient cashServiceClient,
			ITransactionRepository transactionRepository)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			_promotionBonusService =
				promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
			_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
			_transactionRepository =
				transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
		}

		public async ValueTask<MessageResponse> GetAsync(
			DispatchPromotionBonusRequest request,
			CancellationToken cancellationToken)
		{
			var playerCodes = request.Players.Select(x => x.PlayerCode).ToArray();
			var promotionBonusIds = request.Players.Select(x => x.PromotionBonusId).ToArray();

			var promotionPlayers = await _promotionBonusService.GetPromotionBonusPlayersQueries()
				.Include(x => x.PlayerCodeNavigation)
				.Include(x => x.PromotionBonus)
				.Where(x => promotionBonusIds.Contains(x.PromotionBonusId))
				.Where(x => playerCodes.Contains(x.PlayerCode))
				.Where(x => x.SentOn == null).ToArrayAsync(cancellationToken);

			var players = promotionPlayers.Where(x =>
					request.Players.Any(p => p.PlayerCode == x.PlayerCode && p.PromotionBonusId == x.PromotionBonusId))
				.ToArray();

			var playerBuckets = players.Select(x => new
			{
				x.PlayerCodeNavigation.BrandId,
				x.PlayerCode,
				x.PlayerCodeNavigation.DefaultWalletId,
				BucketAmount = x.BonusAmount * x.PromotionBonus.TurnoverMultiple,
				x.BonusAmount,
				BucketType = BucketType.Promotion,
				x.PromotionBonus.PromotionId,
				x.PromotionBonusId,
				x.PromotionBonus.PromotionType
			})
				.ToArray();

			foreach (var promotionPlayer in players)
			{
				promotionPlayer.SentOn = DateTime.UtcNow;
			}

			await _service.CreatePlayerBucketsAsync(playerBuckets.Select(x =>
					new CreatePlayerBucketRequest(x.BrandId,
						x.PlayerCode,
						x.DefaultWalletId,
						x.BucketType,
						x.BucketAmount))
				.ToArray()).ConfigureAwait(false);

			await _promotionBonusService.UpdatePromotionBonusPlayersAsync(players);

			var transactionPromotions = new List<TransactionPromotion>();

			foreach (var promotionPlayer in playerBuckets)
			{
				var response = await _cashServiceClient
					.PromotionAsync(
						promotionPlayer.DefaultWalletId,
						promotionPlayer.BonusAmount,
						null,
						null,
						request.OperatorUserId)
					.ConfigureAwait(false);

				transactionPromotions.Add(new()
				{
					TransactionId = response.TransactionId,
					PromotionId = promotionPlayer.PromotionId,
					PromotionBonusId = promotionPlayer.PromotionBonusId,
					PromotionType = promotionPlayer.PromotionType
				});
			}

			await _transactionRepository.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());

			return new MessageResponse { Valid = true };
		}
	}
}
