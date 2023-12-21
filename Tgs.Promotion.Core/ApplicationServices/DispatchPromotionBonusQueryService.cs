using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class DispatchPromotionBonusQueryService(
	IPromotionBonusRepository promotionBonusService,
	ICashServiceClient cashServiceClient,
	ITransactionRepository transactionRepository,
	IPlayerBucketForTypesClient playerBucketForTypesClient) : IGetService<DispatchPromotionBonusQuery, MessageResponse>
{
	private readonly ICashServiceClient _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
	private readonly IPromotionBonusRepository _promotionBonusService =
			promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
	private readonly ITransactionRepository _transactionRepository =
			transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));

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
			.Where(x => x.SentOn == null)
			.ToArrayAsync(cancellationToken);

		var players = promotionPlayers.Where(x =>
				request.Players.Any(p => p.PlayerCode == x.PlayerCode && p.PromotionBonusId == x.PromotionBonusId))
			.ToArray();

		var playerBuckets = players
			.Select(x => new
			{
				x.Id,
				x.PlayerCodeNavigation.BrandId,
				x.PlayerCode,
				x.PlayerCodeNavigation.DefaultWalletId,
				BucketAmount =
					x.BucketAmountByManual > 0
						? x.BucketAmountByManual
						: x.BonusAmount * x.PromotionBonus.TurnoverMultiple,
				x.BonusAmount,
				BucketType = BucketType.Promotion,
				x.PromotionBonus.PromotionId,
				x.PromotionBonusId,
				x.PromotionBonus.PromotionType,
				x.PromotionBonus.Detail
			})
			.ToArray();

		var playerIds = players.Select(x => x.Id);

		await _promotionBonusService.UpdatePromotionBonusPlayersAsync(playerIds);

		foreach (var playerBucket in playerBuckets)
		{
			var promotionRule = playerBucket.Detail == null
				? null
				: JsonSerializer.Deserialize<PromotionCampaignDetailRule>(playerBucket.Detail,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			var detail = promotionRule == null
				? null
				: JsonSerializer.Deserialize<DetailBucketType?>(promotionRule.CampaignDetail,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			await playerBucketForTypesClient.CreateBucketForBucketTypesAsync(
					playerBucket.PlayerCode,
					playerBucket.BucketAmount,
					playerBucket.BucketType,
					playerBucket.PromotionId.ToString(),
					null,
					detail?.BucketTypes,
					cancellationToken)
				.ConfigureAwait(false);
		}

		var transactionPromotions = new List<TransactionPromotion>();

		foreach (var promotionPlayer in playerBuckets)
		{
			var response = await _cashServiceClient
				.PromotionAsync(
					promotionPlayer.DefaultWalletId,
					promotionPlayer.BonusAmount,
					null,
					promotionPlayer.Id.ToString(),
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

		await _transactionRepository.CreateTransactionPromotionsAsync([.. transactionPromotions]);

		return new MessageResponse { Valid = true };
	}
}
