using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionHistoryController : ControllerBase
{
	[HttpGet("detail/{promotionId}/{eventId}", Name = "GetEventDetail")]
	public async ValueTask<EventDetailViewModel?> GetEventDetailAsync(
		[FromServices] IGetService<EventDetailQuery, EventInfo?> getService,
		Guid promotionId,
		Guid eventId)
	{
		var eventInfo = await getService.GetAsync(
				new EventDetailQuery(promotionId, eventId),
				HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return eventInfo is null
			? null 
			: new EventDetailViewModel { ChangeSets = eventInfo.ChangeSets };
	}
}