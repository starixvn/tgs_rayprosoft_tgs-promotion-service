using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tgs.CashService.Client;
using Tgs.Promotion.Core.Extensions;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.PromotionBonus;
using Tgs.Promotion.Domain.Models.PromotionBucket;
using Tgs.Promotion.Domain.shared;
using Tgs.Promotion.Repository;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Handlers.Promotion;

public class
    CreatePromotionBonusWeeklyHandler : IGetService<CreatePromotionBonusWeeklyRequest,
        MessageResponse>
{
    private readonly ITgsService<BrandGame> _brandGameService;
    private readonly ICashServiceClient _cashServiceClient;

    private readonly IPlayerBucketRepository _playerBucketFactory;
    private readonly ITgsService<PlayerGameRebateAggregate> _playerGameRebateAggregateService;
    private readonly ITgsService<Player> _playerService;
    private readonly IPromotionBonusRepository _promotionBonusFactory;
    private readonly ITgsService<PromotionBonus> _promotionBonusService;
    private readonly IPromotionRepository _promotionFactory;
    private readonly ITgsService<Domain.Entities.Promotion> _promotionService;
    private readonly ITransactionRepository _transactionFactory;

    public CreatePromotionBonusWeeklyHandler(
        IPromotionBonusRepository promotionBonusFactory,
        IPlayerBucketRepository playerBucketFactory,
        ICashServiceClient cashServiceClient,
        ITransactionRepository transactionFactory,
        IPromotionRepository promotionFactory,
        ITgsService<Domain.Entities.Promotion> promotionService,
        ITgsService<PromotionBonus> promotionBonusService,
        ITgsService<PlayerGameRebateAggregate> playerGameRebateAggregateService,
        ITgsService<BrandGame> brandGameService,
        ITgsService<Player> playerService)
    {
        _promotionBonusFactory =
            promotionBonusFactory ?? throw new ArgumentNullException(nameof(promotionBonusFactory));
        _playerBucketFactory = playerBucketFactory ?? throw new ArgumentNullException(nameof(playerBucketFactory));
        _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
        _promotionFactory = promotionFactory ?? throw new ArgumentNullException(nameof(promotionFactory));
        _transactionFactory =
            transactionFactory ?? throw new ArgumentNullException(nameof(transactionFactory));
        _promotionService =
            promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        _promotionBonusService =
            promotionBonusService ?? throw new ArgumentNullException(nameof(promotionBonusService));
        _playerGameRebateAggregateService =
            playerGameRebateAggregateService ??
            throw new ArgumentNullException(nameof(playerGameRebateAggregateService));
        _brandGameService =
            brandGameService ?? throw new ArgumentNullException(nameof(brandGameService));
        _playerService =
            playerService ?? throw new ArgumentNullException(nameof(playerService));
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

        var previousMonday = now.AddDays(-7).GetSunday();
        var previousSunday = now.AddDays(-7).GetMonday();


        var promotions = await _promotionService.GetQueries()
            .Where(x => x.Status == (short)PromotionStatus.Enabled)
            .Where(x => x.SettlementIntervalType == "Weekly")
            .Where(x => x.From >= previousMonday && x.From <= previousSunday ||
                x.To >= previousMonday && x.To <= previousSunday ||
                x.From < previousMonday && x.To > previousSunday
            ).ToArrayAsync(cancellationToken);

        var promotionIds = promotions.Select(x => x.Id);

        if (await _promotionBonusService.GetQueries()
                .AnyAsync(
                    x => promotionIds.Contains(x.PromotionId) && previousMonday <= x.From &&
                        previousSunday >= x.To,
                    cancellationToken))
        {
            return new()
            {
                Valid = false,
                Message =
                    $"Promotion bonus report date range {previousMonday} - {previousSunday} is already existed Weekly."
            };
        }

        var gameProviderIds = promotions.SelectMany(x => x.PromotionGameProviders).Select(x => x.GameProviderId)
            .ToArray();

        var gameTypes = promotions.Select(x => x.GameType).ToArray();

        var playerGameRoundAggregate = await _playerGameRebateAggregateService.GetQueries()
            .Where(x => x.ReportDate >= previousMonday && x.ReportDate <= previousSunday)
            .Where(x => gameProviderIds.Contains(x.GameProviderId))
            .Where(!gameTypes.Contains(null), x => gameTypes.Contains(x.GameType))
            .GroupJoin(_brandGameService.GetQueries(),
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

        var players = await _playerService.GetQueries()
            .Where(x => playerCodes.Contains(x.PlayerCode))
            .Select(x => new
            {
                x.BrandId,
                x.PlayerCode,
                x.DefaultWalletId
            })
            .ToArrayAsync(cancellationToken);

        await _promotionFactory.UpdatePromotionsAsync(promotions);
        var bonus = await _promotionBonusFactory.CreatePromotionBonusAsync(promotionBonus);

        var playerBuckets = autoSentPlayerBonuses
            .SelectMany(x =>
            {
                return x.Players.Join(players,
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

        await _playerBucketFactory.CreatePlayerBucketsAsync(playerBuckets.Select(x =>
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

        await _transactionFactory.CreateTransactionPromotionsAsync(transactionPromotions.ToArray());

        return new()
            { Valid = true };
    }

    private static (PromotionBonusPlayerModel[], decimal) GetPromotionBonusPlayers(
        Guid id,
        Domain.Entities.Promotion promotion, IEnumerable<PlayerGameRebateAggregate> aggregates,
        DateTimeOffset from, DateTimeOffset to)
    {
        var amount = promotion.AmountUsed;

        var promotionLevels = promotion.PromotionVipLevels.Select(v => v.Level).ToArray();

        var players = aggregates
            .Where(promotionLevels.Length > 0,
                pa => pa.VipLevel != null && promotionLevels.Contains(pa.VipLevel.Value))
            .Where(promotion.GameType is not null, pa => pa.GameType == promotion.GameType)
            .Where(pa =>
                promotion.PromotionGameProviders.Select(pg => pg.GameProviderId)
                    .Contains(pa.GameProviderId))
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
                            PlayerCode = p.PlayerCode!,
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
                            PlayerCode = p.PlayerCode!,
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
                        PlayerCode = p.PlayerCode!,
                        BonusAmount = playerRebateAmount,
                        PromotionType = promotion.PromotionType
                    };
                })
                .ToArray(), amount),
            _ => (Array.Empty<PromotionBonusPlayerModel>(), 0)
        };
    }
}