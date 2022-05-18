using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionDispatchController : ControllerBase
{
	[HttpGet]
	[Description("Get promotion dispatches")]
	public ValueTask<PagedResponse<PromotionDispatchModel>> GetPromotionsAsync(
		[FromServices] IGetService<GetPromotionDispatchesRequest, PagedResponse<PromotionDispatchModel>> getService,
		[FromQuery] GetPromotionDispatchesRequest request) =>
		getService.GetAsync(request);

	[HttpPost]
	[Description("Dispatch promotion")]
	public ValueTask<MessageResponse> DispatchPromotionsAsync(
		[FromServices] IGetService<DispatchPromotionBonusRequest, MessageResponse> getService,
		[FromBody] DispatchPromotionBonusRequest request) =>
		getService.GetAsync(request);
}
