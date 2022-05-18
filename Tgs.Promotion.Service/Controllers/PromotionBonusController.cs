using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionBonusController : ControllerBase
{
	[HttpPost("settleDataDaily")]
	[Description("settleDataDaily")]
	public ValueTask<MessageResponse> SettleDataDailyAsync(
		[FromServices] IGetService<CreatePromotionBonusDailyRequest, MessageResponse> getService,
		CreatePromotionBonusDailyRequest request) => getService.GetAsync(request);

	[HttpPost("settleDataWeekly")]
	[Description("settleDataWeekly")]
	public ValueTask<MessageResponse> SettleDataWeeklyAsync(
		[FromServices] IGetService<CreatePromotionBonusWeeklyRequest, MessageResponse> getService,
		CreatePromotionBonusWeeklyRequest request) => getService.GetAsync(request);

	[HttpPost("settleDataMonthly")]
	[Description("settleDataMonthly")]
	public ValueTask<MessageResponse> SettleDataMonthlyAsync(
		[FromServices] IGetService<CreatePromotionBonusMonthlyRequest, MessageResponse> getService,
		CreatePromotionBonusMonthlyRequest request) => getService.GetAsync(request);

	[HttpGet("bonusAmount")]
	[Description("Get bonus amount by ids")]
	public ValueTask<PromotionBonusOfDepositModel[]> GetBonusAmountByIdsAsync(
		[FromServices] IQueryService<GetPromotionBonusOfDepositQuery, PromotionBonusOfDepositModel> queryService,
		[FromQuery] GetPromotionBonusOfDepositQuery query)
		=> queryService.QueryAsync(query).ToArrayAsync();
}
