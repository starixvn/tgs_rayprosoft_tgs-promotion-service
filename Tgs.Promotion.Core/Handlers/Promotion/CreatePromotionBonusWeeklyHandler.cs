using System.Linq;
using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.PromotionBonus;
using Tgs.Promotion.Domain.Models.PromotionBucket;
using Tgs.Promotion.Domain.shared;
using Tgs.ServiceUtility;
using Tgs.Promotion.Core.Extensions;

namespace Tgs.Promotion.Core.Handlers.Promotion
{
	public class
		CreatePromotionBonusWeeklyHandler : IGetService<CreatePromotionBonusWeeklyRequest,
			MessageResponse>
	{
		private readonly ICashServiceClient _cashServiceClient;
		private readonly IPlayerBucketRepository _playerBucketService;
		private readonly ITgsService<PromotionBonus> _promotionBonusService;
		private readonly IPromotionRepository _promotionRepository;
		private readonly ITgsRepository _repository;
		private readonly IPromotionBonusRepository _promotionBonusFactory;
		private readonly ITransactionRepository _transactionRepository;

		public CreatePromotionBonusWeeklyHandler(
			ITgsService<PromotionBonus> promotionBonusService,
			IPlayerBucketRepository playerBucketService,
			ICashServiceClient cashServiceClient,
			ITgsRepository repository,
			IPromotionRepository promotionRepository,
			IPromotionBonusRepository promotionBonusFactory,
			ITransactionRepository transactionRepository)
		{
			_promotionBonusFactory =
				promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
			_promotionBonusService =
				promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
			_playerBucketService = playerBucketService ?? throw new ArgumentNullException(nameof(playerBucketService));
			_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
			_transactionRepository =
				transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
		}

		public async ValueTask<MessageResponse> GetAsync(
			CreatePromotionBonusWeeklyRequest request,
			CancellationToken cancellationToken)
		{
			var now = new DateTimeOffset(DateTimeOffset.Now.Year,
				DateTimeOffset.Now.Month,
				DateTimeOffset.Now.Day,
				0,
				0,
				0,
				new(8, 0, 0));

			var previousSunday = now.AddDays(-7).GetSunday();
			var previousMonday = now.AddDays(-7).GetMonday();

			var promotions = await _promotionRepository.GetPromotionQueries()
				.Where(x => x.Status == (short)PromotionStatus.Enabled)
				.Where(x => x.SettlementIntervalType == "Weekly")
				.Where(x => x.From >= previousMonday.UtcDateTime && x.From <= previousSunday.UtcDateTime ||
					x.To >= previousMonday.UtcDateTime && x.To <= previousSunday.UtcDateTime ||
					x.From < previousMonday.UtcDateTime && x.To > previousSunday.UtcDateTime
				).ToArrayAsync(cancellationToken);

			var promotionIds = promotions.Select(x => x.Id);

			if (await _promotionBonusService.GetQueries()
					.AnyAsync(
						x => promotionIds.Contains(x.PromotionId) && previousMonday.UtcDateTime <= x.From &&
							previousSunday.UtcDateTime >= x.To,
						cancellationToken))
			{
				return new()
				{
					Valid = false,
					Message =
						$"Promotion bonus report date range {previousMonday} - {previousSunday} is already existed Weekly."
				};
			}

			var gameTypes = promotions.Select(x => x.GameType).ToArray();

			var playerGameRoundAggregate = await _repository.PlayerGameRebateAggregates
				.Where(x => x.ReportDate >= previousMonday.UtcDateTime && x.ReportDate <= previousSunday.UtcDateTime)
				.Where(!gameTypes.Contains(null), x => gameTypes.Contains(x.GameType))
				.GroupJoin(_repository.BrandGames,
					aggregate => new
					{
						aggregate.BrandId,
						aggregate.GameId
					},
					brandGame => new
					{
						brandGame.BrandId,
						brandGame.GameId
					},
					(aggregate, brandGame) => new
					{
						aggregate,
						brandGame
					})
				.SelectMany(x => x.brandGame.DefaultIfEmpty(),
					(x, brandGame) => new
					{
						x.aggregate,
						brandGame
					})
				.Where(x => x.brandGame == null || x.brandGame.Rebate).ToArrayAsync(cancellationToken);

			var promotionBonus = promotions.Select(x =>
			{
				var id = GuidExtensions.NewSequentialGuid();
				var from = x.From > previousMonday ? x.From : previousMonday;
				var to = x.To < previousSunday ? x.To : previousSunday;

				var (players, amountUsed) =
					GetPromotionBonusPlayers(id, x, playerGameRoundAggregate.Select(p => p.aggregate), from, to);
				x.AmountUsed = amountUsed;

				return new PromotionBonusModel
				{
					Id = id,
					BrandId = x.BrandId,
					PromotionId = x.Id,
					From = from,
					To = to,
					PromotionType = x.PromotionType,
					TotalAmountLimit = x.TotalAmountLimit,
					GameType = (GameType?)x.GameType,
					Percentage = x.Percentage,
					BonusAmountLimit = x.BonusAmountLimit,
					TurnoverMultiple = x.TurnoverMultiple,
					CreatedOn = DateTimeOffset.Now,
					SentOn = x.AutoSentPlayerBonus ? DateTimeOffset.Now : null,
					Players = players.Where(p => p.BonusAmount > 0M).ToArray()
				};
			}).ToArray();

			var autoSentPlayerBonuses = promotionBonus.Where(x => x.AutoSentPlayerBonus).ToArray();
			var playerCodes = autoSentPlayerBonuses.SelectMany(p => p.Players).Select(p => p.PlayerCode);

			var players = await _repository.Players.AsQueryable()
				.Where(x => playerCodes.Contains(x.PlayerCode))
				.Select(x => new
				{
					x.BrandId,
					x.PlayerCode,
					x.DefaultWalletId
				})
				.ToArrayAsync(cancellationToken);

			await _promotionRepository.UpdatePromotionsAsync(promotions);
			var bonus = await _promotionBonusFactory.CreatePromotionBonusAsync(promotionBonus);

			var playerBuckets = bonus
				.SelectMany(x =>
				{
					return x.PromotionPlayers.Join(players,
							pb => pb.PlayerCode,
							p => p.PlayerCode,
							(pb, p) => new
							{
								pb,
								p
							})
						.Select(p => new
						{
							p.pb.Id,
							x.BrandId,
							p.p.PlayerCode,
							p.p.DefaultWalletId,
							BucketAmount = p.pb.BonusAmount * x.TurnoverMultiple,
							p.pb.BonusAmount,
							BucketType = BucketType.Promotion,
							x.PromotionId,
							PromotionBonusId = x.Id,
							x.PromotionType
						});
				}).ToArray();

			await _playerBucketService.CreatePlayerBucketsAsync(playerBuckets.Select(x =>
				new CreatePlayerBucketRequest(x.BrandId,
					x.PlayerCode,
					x.DefaultWalletId,
					x.BucketType,
					x.BucketAmount)).ToArray()).ConfigureAwait(false);

			var transactionPromotions = new List<TransactionPromotion>();

			foreach (var createPlayerBucketRequest in playerBuckets)
			{
				var response = await _cashServiceClient
					.PromotionAsync(
						createPlayerBucketRequest.DefaultWalletId,
						createPlayerBucketRequest.BonusAmount,
						null,
						createPlayerBucketRequest.Id.ToString(),
						null)
					.ConfigureAwait(false);

				transactionPromotions.Add(new()
				{
					TransactionId = response.TransactionId,
					PromotionId = createPlayerBucketRequest.PromotionId,
					PromotionBonusId = createPlayerBucketRequest.PromotionBonusId,
					PromotionType = createPlayerBucketRequest.PromotionType
				});
			}

			await _transactionRepository.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());

			return new MessageResponse{Valid=true};
		}

		private static (PromotionBonusPlayerModel[], decimal) GetPromotionBonusPlayers(
			Guid id,
			Domain.Entities.Promotion promotion, IEnumerable<PlayerGameRebateAggregate> aggregates,
			DateTimeOffset from, DateTimeOffset to)
		{
			var amount = promotion.AmountUsed;

			var promotionLevels = promotion.PromotionVipLevels.Select(v => v.Level).ToArray();

			var promotionGameProviderIds = promotion.PromotionGameProviders.Select(pg => pg.GameProviderId).ToArray();

			var players = aggregates
				.Where(promotionLevels.Length > 0,
					pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
				.Where(promotion.GameType is not null, pa => pa.GameType == promotion.GameType)
				.Where(promotionGameProviderIds.Length > 0,
					pa =>
						promotionGameProviderIds.Contains(pa.GameProviderId))
				.Where(pa => pa.ReportDate >= from)
				.Where(pa => pa.ReportDate >= to);

			return promotion.PromotionType switch
			{
				"ReliefMoneyWeekly" => (promotion.PromotionParticipants.Select(p =>
				{
					if (amount > promotion.TotalAmountLimit && promotion.Unlimited == 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = p.PlayerCode,
							BonusAmount = 0M,
							PromotionType = promotion.PromotionType
						};
					}

					var playerRebateAmount =
						players.Where(pa => pa.PlayerCode == p.PlayerCode).Sum(s => s.WinLoss) *
						promotion.Percentage / 100;

					if (playerRebateAmount <= 0)
					{
						return new PromotionBonusPlayerModel
						{
							PromotionBonusId = id,
							PlayerCode = p.PlayerCode,
							BonusAmount = 0,
							PromotionType = promotion.PromotionType
						};
					}

					playerRebateAmount = playerRebateAmount > promotion.BonusAmountLimit
						? promotion.BonusAmountLimit
						: playerRebateAmount;

					amount += playerRebateAmount;

					return new PromotionBonusPlayerModel
					{
						PromotionBonusId = id,
						PlayerCode = p.PlayerCode,
						BonusAmount = playerRebateAmount,
						PromotionType = promotion.PromotionType
					};
				})
					.ToArray(), amount),
				_ => (Array.Empty<PromotionBonusPlayerModel>(), 0)
			};
		}
	}
}