using System.Text.Json;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models.Promotion;
using Tgs.Promotion.Domain.shared;
using Tgs.ServiceUtility;
using Microsoft.EntityFrameworkCore;

namespace Tgs.Promotion.Core.Services
{
    internal class GetValidPromotionsQueryService : IQueryService<GetValidPromotionsQuery, PromotionModel>
    {
        private readonly IPromotionRepository _promotionRepository;

        public GetValidPromotionsQueryService(
            IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
        }

        public IAsyncEnumerable<PromotionModel> QueryAsync(
            GetValidPromotionsQuery query, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.Now;

            var promotionQueryable = _promotionRepository.GetPromotionQueries()
                .Where(x => x.BrandId == query.BrandId)
                .Where(x => x.Unlimited == 1 || x.TotalAmountLimit > x.AmountUsed)
                .Where(x => x.Status == (short)PromotionStatus.Enabled
                    && x.From <= now
                    && x.To > now)
                .Select(x => new PromotionModel
                {
                    Id = x.Id,
                    BrandId = x.BrandId,
                    From = x.From,
                    To = x.To,
                    Status = (PromotionStatus)x.Status,
                    Name = x.Name,
                    AutoSentPlayerBonus = x.AutoSentPlayerBonus,
                    PromotionType = x.PromotionType,
                    BonusAmountLimit = x.BonusAmountLimit,
                    AmountUsed = x.AmountUsed,
                    Unlimited = x.Unlimited == 1,
                    GameType = (GameType?)x.GameType,
                    Percentage = x.Percentage,
                    TotalAmountLimit = x.TotalAmountLimit,
                    TurnoverMultiple = x.TurnoverMultiple,
                    GameProviderIds = x.PromotionGameProviders.Select(g => g.GameProviderId).ToArray(),
                    VipLevels = x.PromotionVipLevels.Select(v => v.Level).OrderBy(level => level).ToArray(),
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.UpdatedOn,
                    CreatedByUsername = x.CreatedByUsername,
                    UpdatedByUsername = x.UpdatedByUsername,
                    SubSubject = x.SubSubject,
                    Content = x.Content,
                    Terms = x.Terms,
                    DesktopBanner = x.DesktopBanner,
                    MobileBanner = x.MobileBanner,
                    Localization = x.Localization == null ? null : JsonDocument.Parse(x.Localization, new()),
                    Number = x.DisplayOrder
                });

            return promotionQueryable.ToArray().ToAsyncEnumerable();
        }
    }
}
