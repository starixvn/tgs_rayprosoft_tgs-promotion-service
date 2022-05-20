using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Tgs.MessageQueue;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Clients.Protos;
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

	[HttpPatch("{promotionId}", Name = "UpdatePromotionMainInfo")]
	public ValueTask UpdatePromotionMainInfoAsync(
		[FromServices] ICommandService<UpdatePromotionMainInfoCommand> updateService,
		Guid promotionId,
		[FromBody] UpdatePromotionViewModel updateModel)
	{
		var command = new UpdatePromotionMainInfoCommand(
			promotionId,
			updateModel.Name,
			(updateModel.EventDuring.From, updateModel.EventDuring.To),
			updateModel.PromotionType,
			updateModel.PromotionLimitAmount,
			updateModel.IsAutoSent,
			updateModel.Modifier);

		return updateService.ExecuteAsync(command, HttpContext.RequestAborted);
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

	[HttpPut("rule/{promotionId}", Name = "UpdatePromotionRule")]
	public async Task UpdatePromotionRuleAsync(
		[FromServices] IGetService<GetPromotionMainInfoQuery, PromotionMainInfo?> getPromotionService,
		[FromServices] IMessageSender messageSender,
		[FromServices] ICommandService<UpdatePromotionRuleCommand> updateService,
		Guid promotionId,
		[FromBody] PromotionRuleViewModel ruleModel)
	{
		var promotionInfo = await getPromotionService
			.GetAsync(new GetPromotionMainInfoQuery(promotionId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		if (promotionInfo is null)
			return;

		var subject = $"tgs.promotion.{promotionInfo.PromotionType.ToLower()}.setting";

		var request = new CampaignDetailRequest
		{
			PromotionId = promotionId,
			Settings = JsonSerializer.Serialize(ruleModel.CampaignDetail)
		};

		var response = await messageSender
			.RequestAsync(subject, request.ToByteArray(), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var processedResult = CampaignDetailResponse.Parser.ParseFrom(response.Span);

		var command = new UpdatePromotionRuleCommand(
			promotionId,
			JsonDocument.Parse(processedResult.CampaignDetail),
			processedResult.BonusAmountLimit,
			processedResult.Percentage,
			processedResult.TurnoverMultiple,
			string.IsNullOrWhiteSpace(processedResult.GameType)
				? null
				: Enum.Parse<GameType>(processedResult.GameType),
			ruleModel.Modifier);

		await updateService
			.ExecuteAsync(command, HttpContext.RequestAborted)
			.ConfigureAwait(false);
	}

	[HttpPut("status/{promotionId}/{status}", Name = "UpdatePromotionStatus")]
	public ValueTask SetPromotionStatusAsync(
		[FromServices] ICommandService<SetPromotionStatusCommand> updateService,
		Guid promotionId,
		PromotionStatus status,
		[FromHeader(Name = "Modifier")] string modifier)
	{
		var command = new SetPromotionStatusCommand(
			promotionId,
			status,
			modifier);

		return updateService.ExecuteAsync(command, HttpContext.RequestAborted);
	}
}
