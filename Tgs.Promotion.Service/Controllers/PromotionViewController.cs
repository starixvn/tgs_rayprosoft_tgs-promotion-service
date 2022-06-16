using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionViewController : ControllerBase
{
	[HttpGet("{brandId}", Name = "ListPromotions")]
	public IAsyncEnumerable<PromotionListItemViewModel> ListPromotionsAsync(
		[FromServices] IQueryService<ListPromotionsByBrandQuery, PromotionView> queryService,
		Guid brandId)
	{
		var query = new ListPromotionsByBrandQuery(brandId);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(item => new PromotionListItemViewModel
			{
				Id = item.Id,
				Name = item.Name,
				Content = item.Content,
				Convention = item.Convention,
				EventDuring = item.EventDuring,
				PromotionType = item.PromotionType,
				PromotionLimitAmount = item.PromotionLimitAmount,
				MobileBanner = item.MobileBanner,
				PersonalComputerBanner = item.PersonalComputerBanner,
				SubSubject = item.SubSubject,
				UsedAmount = item.UsedAmount,
				VipLevels = item.VipLevels,
				Localization = item.Localization,
				CampaignDetail = item.CampaignDetail
			});
	}

	[HttpGet("{brandId}/{playerCode}", Name = "ListPlayerPromotions")]
	public IAsyncEnumerable<PromotionListItemViewModel> ListPromotionsAsync(
		[FromServices] IQueryService<ListPromotionsByPlayerQuery, PromotionView> queryService,
		Guid brandId,
		string playerCode)
	{
		var query = new ListPromotionsByPlayerQuery(brandId, playerCode);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(item => new PromotionListItemViewModel
			{
				Id = item.Id,
				Name = item.Name,
				Content = item.Content,
				Convention = item.Convention,
				EventDuring = item.EventDuring,
				PromotionType = item.PromotionType,
				PromotionLimitAmount = item.PromotionLimitAmount,
				MobileBanner = item.MobileBanner,
				PersonalComputerBanner = item.PersonalComputerBanner,
				SubSubject = item.SubSubject,
				UsedAmount = item.UsedAmount,
				VipLevels = item.VipLevels,
				Localization = item.Localization,
				CampaignDetail = item.CampaignDetail
			});
	}
}