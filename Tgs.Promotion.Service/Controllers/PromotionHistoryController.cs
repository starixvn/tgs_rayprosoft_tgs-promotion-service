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
		[FromServices] IGetService<GetEventDetailQuery, EventInfo?> getService,
		Guid promotionId,
		Guid eventId)
	{
		var eventInfo = await getService.GetAsync(
				new GetEventDetailQuery(promotionId, eventId),
				HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return eventInfo is null
			? null
			: new EventDetailViewModel
			{
				EventId = eventInfo.EventId,
				PromotionId = eventInfo.PromotionId,
				Action = eventInfo.Action,
				CreatedOn = eventInfo.CreatedOn,
				ChangeSets = eventInfo.ChangeSets,
				Modifier = eventInfo.Modifier
			};
	}

	[HttpGet("{promotionId}", Name = "QueryEventsByPromotionId")]
	public IAsyncEnumerable<EventViewModel> QueryEventsByPromotionIdAsync(
		[FromServices] IQueryService<GetEventsQuery, EventInfo> queryService,
		Guid promotionId)
	{
		var query = new GetEventsQuery(promotionId);

		return queryService.QueryAsync(
				query,
				HttpContext.RequestAborted)
			.Select(evt => new EventViewModel
			{
				EventId = evt.EventId,
				PromotionId = evt.PromotionId,
				Action = evt.Action,
				CreatedOn = evt.CreatedOn,
				Modifier = evt.Modifier
			});
	}
}
