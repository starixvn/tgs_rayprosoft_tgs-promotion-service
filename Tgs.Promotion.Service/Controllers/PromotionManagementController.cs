using System.ComponentModel;
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
	[HttpPut("orders", Name = "ChangePromotionOrders")]
	[Description("Change display order of promotions ")]
	public ValueTask ChangePromotionOrdersAsync(
		[FromServices] ICommandService<ChangePromotionOrdersCommand> commandService,
		[FromBody] PromotionOrdersViewModel promotionOrdersViewModel)
	{
		var orders = promotionOrdersViewModel.Orders
			.Select(p => new PromotionOrder(p.PromotionId, p.Order))
			.ToArray();

		var command = new ChangePromotionOrdersCommand(
			orders,
			promotionOrdersViewModel.Modifier);

		return commandService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

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

	[HttpGet("content/{promotionId}", Name = "GetPromotionContent")]
	public async ValueTask<PromotionContentViewModel?> GetPromotionContentAsync(
		[FromServices] IGetService<GetPromotionContentQuery, PromotionContentDetail?> getService,
		Guid promotionId)
	{
		var query = new GetPromotionContentQuery(promotionId);

		var result = await getService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return result is null
			? null
			: new PromotionContentViewModel
			{
				Content = result.Content,
				Convention = result.Convention,
				Localization = result.Localization,
				SubSubject = result.SubSubject,
				PersonalComputerBanner = result.PersonalComputerBanner,
				MobileBanner = result.MobileBanner
			};
	}

	[HttpGet("{promotionId:Guid}", Name = "GetPromotionMainInfo")]
	public async ValueTask<PromotionViewModel?> GetPromotionMainInfoAsync(
		[FromServices] IGetService<GetPromotionMainInfoQuery, PromotionMainInfo?> getService,
		Guid promotionId)
	{
		var query = new GetPromotionMainInfoQuery(promotionId);

		var result = await getService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return result is null
			? null
			: new PromotionViewModel
			{
				Id = result.Id,
				BrandId = result.BrandId,
				Name = result.Name,
				EventDuring = result.EventDuring,
				IsAutoSent = result.IsAutoSent,
				PromotionLimitAmount = result.PromotionLimitAmount,
				PromotionType = result.PromotionType
			};
	}

	[HttpGet("{promotionType}", Name = "GetPromotionMainInfosByPromotionType")]
	public IAsyncEnumerable<PromotionViewModel> GetPromotionMainInfosByPromotionTypeAsync(
		[FromServices] IQueryService<GetPromotionMainInfosByPromotionTypeQuery, PromotionMainInfo> queryService,
		string promotionType)
	{
		var query = new GetPromotionMainInfosByPromotionTypeQuery(promotionType);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(info => new PromotionViewModel
			{
				Id = info.Id,
				BrandId = info.BrandId,
				EventDuring = info.EventDuring,
				Name = info.Name,
				IsAutoSent = info.IsAutoSent,
				PromotionLimitAmount = info.PromotionLimitAmount,
				PromotionType = info.PromotionType
			});
	}

	[HttpGet("rule/{promotionId}", Name = "GetPromotionRule")]
	public async ValueTask<PromotionRuleViewModel?> GetPromotionRuleAsync(
		[FromServices] IGetService<GetPromotionRuleQuery, PromotionRule?> getService,
		Guid promotionId)
	{
		var query = new GetPromotionRuleQuery(promotionId);

		var rule = await getService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return rule is null
			? null
			: new PromotionRuleViewModel
			{
				CampaignDetail = rule.CampaignDetail,
				BonusAmountLimit = rule.BonusAmountLimit,
				GameType = rule.GameType,
				Percentage = rule.Percentage,
				TurnoverMultiple = rule.TurnoverMultiple,
				VipLevels = rule.VipLevels
			};
	}

	[HttpGet("ids/{brandId}/{promotionName}", Name = "QueryPromotionIds")]
	public IAsyncEnumerable<Guid> QueryPromotionIdsAsync(
		[FromServices] IQueryService<PromotionIdsQuery, Guid> queryService,
		Guid brandId,
		string promotionName,
		[FromQuery(Name = "promotionType")] string? promotionType)
	{
		var query = new PromotionIdsQuery(
			brandId,
			promotionName,
			promotionType);

		return queryService.QueryAsync(query, HttpContext.RequestAborted);
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

	[HttpPut("content/{promotionId}", Name = "UpdatePromotionContent")]
	public ValueTask UpdatePromotionContentAsync(
		[FromServices] ICommandService<UpdatePromotionContentCommand> updateService,
		Guid promotionId,
		[FromBody] UpdatePromotionContentViewModel contentModel)
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

	[HttpPut("rule/{promotionId}", Name = "UpdatePromotionRule")]
	public async Task UpdatePromotionRuleAsync(
		[FromServices] IGetService<GetPromotionMainInfoQuery, PromotionMainInfo?> getPromotionService,
		[FromServices] IMessageSender messageSender,
		[FromServices] ICommandService<UpdatePromotionRuleCommand> updateService,
		Guid promotionId,
		[FromBody] UpdatePromotionRuleViewModel ruleModel)
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
			.RequestAsync(subject, request.ToByteArray())
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

	[HttpPut("used/{promotionId}", Name = "UpdatePromotionUsedAmount")]
	public ValueTask UpdatePromotionUsedAmountAsync(
		[FromServices] ICommandService<UpdatePromotionUsedAmountCommand> updateService,
		Guid promotionId,
		[FromBody] UsedAmountViewModel model)
	{
		var command = new UpdatePromotionUsedAmountCommand(
			promotionId,
			model.TotalAmount,
			model.SnapshotId);

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

	[HttpGet("used/{promotionId}", Name = "GetPromotionUsedAmount")]
	public ValueTask<decimal> GetPromotionUsedAmountAsync(
		[FromServices] IGetService<GetPromotionUsedAmountQuery, decimal> queryService,
		Guid promotionId)
	{
		var query = new GetPromotionUsedAmountQuery(
			promotionId);

		return queryService.GetAsync(query, HttpContext.RequestAborted);
	}
}
