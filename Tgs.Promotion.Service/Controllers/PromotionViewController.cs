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
				CampaignDetail = item.CampaignDetail,
				Order = item.Order,
				Status = item.Status
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
				CampaignDetail = item.CampaignDetail,
				Order = item.Order,
				Status = item.Status
			});
	}

	[HttpGet("{brandId}:filters", Name = "ListPromotionsByFilters")]
	public IAsyncEnumerable<PromotionListItemViewModel> ListPromotionsAsync(
		[FromServices] IQueryService<ListPromotionsByFilters, PromotionView> queryService,
		Guid brandId,
		[FromQuery] ListPromotionByFiltersViewModel viewModel)
	{
		var during = viewModel.From is not null && viewModel.To is not null
			? (viewModel.From.Value, viewModel.To.Value)
			: (During?)null;

		var query = new ListPromotionsByFilters(
			brandId,
			during,
			viewModel.Name,
			viewModel.PromotionTypes ?? Array.Empty<string>(),
			viewModel.Status);

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
				CampaignDetail = item.CampaignDetail,
				Order = item.Order,
				Status = item.Status
			});
	}
}
