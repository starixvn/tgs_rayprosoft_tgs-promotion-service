using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/promotion-mgr")]
[ApiController]
public class PromotionManagementController : ControllerBase
{
	[HttpPut("{brandId}", Name = "CreatePromotionMainInfo")]
	public ValueTask<Guid> CreatePromotionAsync(
			[FromServices] IGetService<CreatePromotionMainInfoCommand, Guid> createService,
			Guid brandId,
			[FromBody] CreatePromotionViewModel createModel)
	{
		var command = new CreatePromotionMainInfoCommand(
			brandId,
			createModel.Name,
			(createModel.EventDuring.From, createModel.EventDuring.To),
			createModel.PromotionType,
			createModel.PromotionLimitAmount,
			createModel.IsAutoSent,
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

	[HttpPut("content/{promotionId}", Name = "UpdatePromotionContent")]
	public ValueTask UpdatePromotionContentAsync(
		[FromServices] ICommandService<UpdatePromotionContentCommand> updateService,
		Guid promotionId,
		[FromBody] PromotionContentViewModel contentModel)
	{
		var command = new UpdatePromotionContentCommand(
			promotionId,
			contentModel.SubSubject,
			contentModel.PersonalComputerBanner,
			contentModel.MobileBanner,
			contentModel.Content,
			contentModel.Convention,
			contentModel.Localization,
			contentModel.Modifier);

		return updateService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
