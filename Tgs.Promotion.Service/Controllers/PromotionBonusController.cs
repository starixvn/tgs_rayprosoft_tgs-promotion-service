using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionBonusController : ControllerBase
{
	[HttpGet("bonusAmount")]
	[Description("Get bonus amount by ids")]
	public ValueTask<PromotionBonusOfDepositModel[]> GetBonusAmountByIdsAsync(
		[FromServices] IQueryService<GetPromotionBonusOfDepositQuery, PromotionBonusOfDepositModel> queryService,
		[FromQuery] GetPromotionBonusOfDepositQuery query)
		=> queryService.QueryAsync(query).ToArrayAsync();
}
