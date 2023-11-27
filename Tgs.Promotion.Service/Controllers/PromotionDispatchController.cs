using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.Promotion.Workers;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionDispatchController : ControllerBase
{
	private readonly IPromotionDispatchTaskQueue _taskQueue;

	public PromotionDispatchController(IPromotionDispatchTaskQueue taskQueue)
	{
		_taskQueue = taskQueue;
	}

	[HttpPost]
	[Description("Dispatch promotion")]
	public async ValueTask<MessageResponse> DispatchPromotionsAsync(
		[FromBody] PromotionDispatchQueueModel request)
	{
		await _taskQueue.QueuePromotionDispatchTaskQueueAsync(request);

		return new MessageResponse { Valid = true };
	}

	[HttpGet]
	[Description("Get promotion dispatches")]
	public ValueTask<PagedResponse<PromotionDispatchModel>> GetPromotionsAsync(
		[FromServices] IGetService<GetPromotionDispatchesQuery, PagedResponse<PromotionDispatchModel>> getService,
		[FromQuery] GetPromotionDispatchesQuery request) =>
		getService.GetAsync(request);
}
