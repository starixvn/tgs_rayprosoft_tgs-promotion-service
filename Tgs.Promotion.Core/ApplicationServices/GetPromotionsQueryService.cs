using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class GetPromotionsQueryService(
	IPromotionRepository service) : IGetService<GetPromotionsQuery,
	PagedResponse<PromotionModel>>
{
	private readonly IPromotionRepository _service = service;

	public async ValueTask<PagedResponse<PromotionModel>> GetAsync(
		GetPromotionsQuery request,
		CancellationToken cancellationToken) =>
		await _service.GetPromotionQueries()
			.Where(x => x.BrandId == request.BrandId)
			.Where(request.Status, x => x.Status == (short?)request.Status)
			.Where(request.Name, x => x.Name == request.Name)
			.Where(request.From, x => x.To >= request.From)
			.Where(request.To, x => x.From <= request.To)
			.Where(x => x.Status != (short)PromotionStatus.Archived)
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
				LocalizationString = x.Localization,
				Localization = x.Localization == null ? null : JsonDocument.Parse(x.Localization, new()),
				Number = x.DisplayOrder
			})
			.AsSingleQuery()
			.CreatePagedResponseAsync(request)
			.ConfigureAwait(false);
}
