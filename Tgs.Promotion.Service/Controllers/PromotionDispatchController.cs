using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionDispatchController : ControllerBase
{
	[HttpPost]
	[Description("Dispatch promotion")]
	public ValueTask<MessageResponse> DispatchPromotionsAsync(
		[FromServices] IGetService<DispatchPromotionBonusQuery, MessageResponse> getService,
		[FromBody] DispatchPromotionBonusQuery request) =>
		getService.GetAsync(request);

	[HttpGet]
	[Description("Get promotion dispatches")]
	public ValueTask<PagedResponse<PromotionDispatchModel>> GetPromotionsAsync(
		[FromServices] IGetService<GetPromotionDispatchesQuery, PagedResponse<PromotionDispatchModel>> getService,
		[FromQuery] GetPromotionDispatchesQuery request) =>
		getService.GetAsync(request);
}
