using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;
using Tgs.Paging;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionController : ControllerBase
{
	[HttpPost("{promotionId}/depositAddPlayer/{playerCode}", Name = "AddPlayerToDepositPromotion")]
	[Description("Add player to deposit promotion")]
	public ValueTask<MessageResponse> AddPlayerToDepositPromotionAsync(
		[FromServices] IGetService<CreateDepositPromotionParticipantQuery, MessageResponse> getService,
		Guid promotionId, string playerCode)
		=> getService.GetAsync(new(promotionId, playerCode));

	[HttpPost("{promotionId}/addPlayer/{playerCode}", Name = "AddPlayerToPromotion")]
	[Description("Add player to promotion")]
	public ValueTask<MessageResponse> AddPlayerToPromotionAsync(
		[FromServices] IGetService<CreatePromotionParticipantQuery, MessageResponse> getService,
		Guid promotionId, string playerCode)
		=> getService.GetAsync(new(promotionId, playerCode));

	[HttpDelete("deposit/{playerCode}", Name = "CancelDepositTag")]
	[HttpDelete("deposit/{playerCode}/{promotionId?}", Name = "CancelDepositTagWithPromotion")]
	public ValueTask CancelDepositAsync(
		[FromServices] ICommandService<CancelTagPlayerDepositCommand> untagService,
		string playerCode,
		Guid? promotionId)
	{
		var command = new CancelTagPlayerDepositCommand(playerCode, promotionId);

		return untagService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpPut("changeorder", Name = "ChangeOrder")]
	[Description("Change promotions display order")]
	public async Task<MessageResponse> ChangeOrderAsync(
		[FromServices] ICommandService<ChangeOrderCommand> commandService,
		ChangeOrderRequest request)
	{
		var command = new ChangeOrderCommand
		{
			Orders = request.Orders
		};

		await commandService.ExecuteAsync(command).ConfigureAwait(false);

		return new()
		{ Valid = true };
	}

	[HttpPut("{promotionId}/{status}", Name = "ChangePromotionStatus")]
	[Description("Change promotion status")]
	public async ValueTask<MessageResponse> ChangePromotionStatusAsync(
		[FromServices] IGetService<ChangePromotionStatusQuery, MessageResponse> getService,
		Guid promotionId, PromotionStatus status)
		=> await getService.GetAsync(new(promotionId, status));

	[HttpGet("{promotionId}/check/{brandId}/{playerCode}/{amount}", Name = "CheckCanJoinPromotion")]
	public async Task<bool> CheckCanJoinPromotionAsync(
		[FromServices] IGetService<CheckCanJoinPromotionQuery, bool> checkService,
		Guid promotionId,
		Guid brandId,
		string playerCode,
		decimal amount)
	{
		var query = new CheckCanJoinPromotionQuery(
			brandId,
			playerCode,
			promotionId,
			amount);

		var result = await checkService.GetAsync(query).ConfigureAwait(false);

		return result;
	}

	[HttpGet("checkjoined/{promotionId}/{playerCode}", Name = "CheckPromotionJoinedAsync")]
	public ValueTask<bool> CheckPromotionJoinedAsync(
		[FromServices] IGetService<CheckPromotionJoinedQuery, bool> checkService,
		string playerCode,
		Guid promotionId)
		=> checkService.GetAsync(new CheckPromotionJoinedQuery(
			playerCode,
			promotionId));

	[HttpPut("deposit/{playerCode}", Name = "TagDeposit")]
	[HttpPut("deposit/{playerCode}/{promotionId?}", Name = "TagDepositWithPromotion")]
	[Description("Tag player deposit")]
	public ValueTask DepositAsync(
		[FromServices] ICommandService<TagPlayerDepositCommand> tagService,
		string playerCode,
		Guid? promotionId)
	{
		var command = new TagPlayerDepositCommand(playerCode, promotionId);

		return tagService.ExecuteAsync(command, HttpContext.RequestAborted);
	}

	[HttpGet("joined")]
	[Description("Get joined promotions")]
	public ValueTask<PagedResponse<JoinedPromotionModel>> GetJoinedPromotionsAsync(
		[FromServices] IGetService<GetJoinedPromotionsQuery, PagedResponse<JoinedPromotionModel>> getService,
		[FromQuery] GetJoinedPromotionsViewModel request)
	{
		var query = new GetJoinedPromotionsQuery
		{
			From = request.From,
			To = request.To,
			PlayerCode= request.PlayerCode,
			Page = request.Page,
			Rows = request.Rows,
			SortBy= request.SortBy,
			OrderBy = request.OrderBy,
			ShowAll = request.ShowAll,
		};

		return getService.GetAsync(query);
	}

	[HttpGet("names", Name = "GetPromotionNamesByIds")]
	[Description("Get promotion names by ids")]
	public ValueTask<PromotionNameModel[]> GetPromotionNamesByIdsAsync(
		[FromServices] IQueryService<GetPromotionNamesQuery, PromotionNameModel> queryService,
		[FromQuery] GetPromotionNamesViewModel query)
		=> queryService.QueryAsync(new(query.PromotionIds)).ToArrayAsync();

	[HttpGet]
	[Description("Get promotions")]
	public ValueTask<PagedResponse<PromotionModel>> GetPromotionsAsync(
		[FromServices] IGetService<GetPromotionsQuery, PagedResponse<PromotionModel>> getService,
		[FromQuery] GetPromotionsQuery request)
		=> getService.GetAsync(request);

	/// <summary>
	/// Get promotions for player
	/// </summary>
	/// <param name="queryByPlayerService">The query by player service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	/// <exception cref="System.InvalidOperationException">Player code is required</exception>
	[HttpGet("valid", Name = "GetPromotionsForPlayer")]
	public async Task<PagedResponse<PromotionModel>> GetPromotionsForPlayerAsync(
		[FromServices] IQueryService<GetValidPromotionsQuery, PromotionModel> queryAllService,
		[FromServices] IQueryService<GetValidPromotionsByPlayerQuery, PromotionModel> queryByPlayerService,
		[FromQuery] GetValidPromotionsViewModel request)
	{
		var promotions = string.IsNullOrWhiteSpace(request.PlayerCode)
			? queryAllService.QueryAsync(new(request.BrandId))
			: queryByPlayerService.QueryAsync(new(
				request.BrandId,
				request.PlayerCode));

		var paging = new Paging<PromotionModel>();

		if (!string.IsNullOrWhiteSpace(request.SortBy))
			paging.SortByPropertyName(request.SortBy, request.IsDesc);

		if (!request.ShowAll)
			return new(await promotions.CountAsync().ConfigureAwait(false), await promotions
				.ApplyOrderBy(paging)
				.ToArrayAsync()
				.ConfigureAwait(false));

		paging.Page = request.Page;
		paging.RowCount = request.Rows;

		return new(await promotions.CountAsync().ConfigureAwait(false), await promotions
			.ApplyOrderBy(paging)
			.ApplyPaging(paging)
			.ToArrayAsync()
			.ConfigureAwait(false));
	}

	[HttpDelete("{promotionId}/{amount}", Name = "RevertPromotion")]
	public async Task RevertPromotionAsync(
		[FromServices] ICommandService<RevertPromotionAmountCommand> revertService,
		Guid promotionId,
		decimal amount)
	{
		if (amount <= 0)
		{
			return;
		}

		var command = new RevertPromotionAmountCommand(promotionId, amount);

		await revertService.ExecuteAsync(command).ConfigureAwait(false);
	}
}
