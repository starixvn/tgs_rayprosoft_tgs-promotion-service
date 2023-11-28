using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.Promotion.Service.ViewModels;
using Tgs.Promotion.Workers;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionDispatchController : ControllerBase
{
	[HttpPost]
	[Description("Dispatch promotion")]
	public async ValueTask<MessageResponse> DispatchPromotionsAsync(
		[FromServices] IPromotionDispatchTaskQueue taskQueue,
		[FromBody] DispatchPromotionBonusViewModel request)
	{
		var model = new PromotionDispatchQueueModel
		{
			OperatorUserId = request.OperatorUserId,
			Players = request.Players.Select(x => new DispatchPlayerQueueModel
				{
					PlayerCode = x.PlayerCode, PromotionBonusId = x.PromotionBonusId
				})
				.ToArray()
		};

		await taskQueue.QueuePromotionDispatchTaskQueueAsync(model);

		return new MessageResponse { Valid = true };
	}

	[HttpGet]
	[Description("Get promotion dispatches")]
	public ValueTask<PagedResponse<PromotionDispatchModel>> GetPromotionsAsync(
		[FromServices] IGetService<GetPromotionDispatchesQuery, PagedResponse<PromotionDispatchModel>> getService,
		[FromQuery] GetPromotionDispatchesQuery request) =>
		getService.GetAsync(request);
}
