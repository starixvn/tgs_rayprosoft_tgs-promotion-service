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
	public ValueTask<Guid> CreatePromotionAsync(
			[FromServices] IGetService<CreatePromotionCommand, Guid> createService,
			Guid brandId,
			[FromBody] CreatePromotionViewModel createModel)
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

		return createService.GetAsync(command, HttpContext.RequestAborted);
	}

	[HttpPut("vip/{promotionId}", Name = "UpdatePromotionVipLevels")]
	public ValueTask UpdatePromotionVipLevelsAsync(
		[FromServices] ICommandService<UpdatePromotionVipLevelsCommand> updateService,
		Guid promotionId,
		[FromBody] VipLevelsViewModel vipLevelsModel)
	{
		var command = new UpdatePromotionVipLevelsCommand(
			promotionId,
			vipLevelsModel.VipLevels,
			vipLevelsModel.Modifier);

		return updateService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
