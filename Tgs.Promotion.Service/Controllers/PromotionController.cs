using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using TGS.Paging;
using Tgs.Promotion.Core.Services;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.Promotion;
using Tgs.Promotion.Domain.shared;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionController : ControllerBase
{
    [HttpPost("{promotionId}/depositAddPlayer/{playerCode}", Name = "AddPlayerToDepositPromotion")]
    [Description("Add player to deposit promotion")]
    public ValueTask<MessageResponse> AddPlayerToDepositPromotionAsync(
        [FromServices] IGetService<CreateDepositPromotionParticipantRequest, MessageResponse> getService,
        Guid promotionId, string playerCode)
        => getService.GetAsync(new(promotionId, playerCode));

    [HttpPost("{promotionId}/addPlayer/{playerCode}", Name = "AddPlayerToPromotion")]
    [Description("Add player to promotion")]
    public ValueTask<MessageResponse> AddPlayerToPromotionAsync(
        [FromServices] IGetService<CreatePromotionParticipantRequest, MessageResponse> getService,
        Guid promotionId, string playerCode)
        => getService.GetAsync(new(promotionId, playerCode));

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
    public ValueTask<MessageResponse> ChangePromotionStatusAsync(
        [FromServices] IGetService<ChangePromotionStatusRequest, MessageResponse> getService,
        Guid promotionId, PromotionStatus status)
        => getService.GetAsync(new(promotionId, status));

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

    [HttpPost(Name = "CreatePromotion")]
    [Description("Create a promotion")]
    public ValueTask<MessageResponse> CreatePromotionAsync(
        [FromServices] IGetService<CreatePromotionRequest, MessageResponse> getService,
        CreatePromotionRequest request) =>
        getService.GetAsync(request);

    [HttpPut("deposit/{playerCode}")]
    [HttpPut("deposit/{playerCode}/{promotionId?}")]
    [Description("Tag player deposit")]
    public async Task DepositAsync(
        [FromServices] ICommandService<TagPlayerDepositCommand> tagService,
        string playerCode,
        Guid? promotionId)
    {
        var command = new TagPlayerDepositCommand(playerCode, promotionId);

        await tagService.ExecuteAsync(command).ConfigureAwait(false);
    }

    [HttpGet("names", Name = "GetPromotionNamesByIds")]
    [Description("Get promotion names by ids")]
    public ValueTask<PromotionNameModel[]> GetPromotionNamesByIdsAsync(
        [FromServices] IQueryService<GetPromotionNamesQuery, PromotionNameModel> queryService,
        [FromQuery] GetPromotionNamesQuery query)
        => queryService.QueryAsync(query).ToArrayAsync();

    [HttpGet]
    [Description("Get promotions")]
    public ValueTask<PagedResponse<PromotionModel>> GetPromotionsAsync(
        [FromServices] IGetService<GetPromotionsRequest, PagedResponse<PromotionModel>> getService,
        [FromQuery] GetPromotionsRequest request)
        => getService.GetAsync(request);

    [HttpGet("joined")]
    [Description("Get joined promotions")]
    public ValueTask<PagedResponse<JoinedPromotionModel>> GetJoinedPromotionsAsync(
        [FromServices] IGetService<GetJoinedPromotionsRequest, PagedResponse<JoinedPromotionModel>> getService,
        [FromQuery] GetJoinedPromotionsRequest request)
        => getService.GetAsync(request);

    [HttpGet("valid", Name = "GetPromotionsForPlayer")]
    [Description("Get promotions fro player")]
    public async Task<PagedResponse<PromotionModel>> GetPromotionsForPlayerAsync(
        [FromServices] IQueryService<GetValidPromotionsQuery, PromotionModel> queryAllService,
        [FromServices] IQueryService<GetValidPromotionsByPlayerQuery, PromotionModel> queryByPlayerService,
        [FromQuery] GetValidPromotionsRequest request)
    {
        var promotions = string.IsNullOrWhiteSpace(request.PlayerCode)
            ? queryAllService.QueryAsync(new(request.BrandId))
            : queryByPlayerService.QueryAsync(new(
                request.BrandId,
                request.PlayerCode));

        var paging = new Paging<PromotionModel>();

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            paging.SortByPropertyName(request.SortBy, request.IsDesc);
        }

        if (!request.ShowAll)
        {
            return new()
            {
                TotalCount = await promotions.CountAsync().ConfigureAwait(false),
                Items = await promotions
                    .ApplyOrderBy(paging)
                    .ToArrayAsync()
                    .ConfigureAwait(false)
            };
        }

        paging.Page = request.Page;
        paging.RowCount = request.Rows;

        return new()
        {
            TotalCount = await promotions.CountAsync().ConfigureAwait(false),
            Items = await promotions
                .ApplyOrderBy(paging)
                .ApplyPaging(paging)
                .ToArrayAsync()
                .ConfigureAwait(false)
        };
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

    [HttpPut("{id}", Name = "UpdatePromotion")]
    [Description("Update a promotion")]
    public ValueTask<MessageResponse> UpdatePromotionAsync(
        [FromServices] IGetService<UpdatePromotionRequest, MessageResponse> getService,
        Guid id, UpdatePromotionRequest request)
    {
        request.Id = id;

        return getService.GetAsync(request);
    }
}