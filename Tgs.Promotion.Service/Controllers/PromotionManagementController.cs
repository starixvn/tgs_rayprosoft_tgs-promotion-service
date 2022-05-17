using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/promotion-mgr")]
[ApiController]
public class PromotionManagementController : ControllerBase
{
	[HttpPut("{brandId}", Name = "CreatePromotionInfo")]
	public ValueTask CreatePromotionAsync(
		[FromServices] ICommandService<CreatePromotionCommand> createService,
		Guid brandId,
		CreatePromotionViewModel createModel)
	{
		var command = new CreatePromotionCommand(
			brandId,
			createModel.Name,
			(createModel.EventDuring.From, createModel.EventDuring.To),
			createModel.PromotionType,
			createModel.PromotionLimitAmount,
			createModel.IsAutoSent,
			createModel.CampaignDetail,
			createModel.Modifier);

		return createService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
