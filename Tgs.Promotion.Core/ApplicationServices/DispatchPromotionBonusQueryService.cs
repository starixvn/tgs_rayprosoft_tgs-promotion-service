using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class DispatchPromotionBonusQueryService : IGetService<DispatchPromotionBonusQuery, MessageResponse>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPromotionBonusRepository _promotionBonusService;
	private readonly ITransactionRepository _transactionRepository;

	public DispatchPromotionBonusQueryService(
		IPromotionBonusRepository promotionBonusService,
		ICashServiceClient cashServiceClient,
		ITransactionRepository transactionRepository,
		IPlayerBucketClient playerBucketClient)
	{
		_promotionBonusService =
			promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_transactionRepository =
			transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask<MessageResponse> GetAsync(
		DispatchPromotionBonusQuery request,
		CancellationToken cancellationToken = default)
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

		var playerBuckets = players
			.Select(x => new
			{
				x.PlayerCodeNavigation.BrandId,
				x.PlayerCode,
				x.PlayerCodeNavigation.DefaultWalletId,
				BucketAmount = x.BucketAmountByManual > 0 ?
					x.BucketAmountByManual :
					x.BonusAmount * x.PromotionBonus.TurnoverMultiple,
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

		foreach (var playerBucket in playerBuckets)
		{
			_ = await _playerBucketClient.CreateBucketAsync(
					playerBucket.PlayerCode,
					playerBucket.BucketAmount,
					playerBucket.BucketType,
					playerBucket.PromotionId.ToString(),
					cancellationToken)
				.ConfigureAwait(false);
		}

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
					request.OperatorUserId,
					cancellationToken)
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
