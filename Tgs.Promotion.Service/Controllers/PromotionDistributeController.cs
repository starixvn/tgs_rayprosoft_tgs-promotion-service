using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PromotionDistributeController : ControllerBase
{
	[HttpPost]
	[Description("Distribute the bonus of promotion")]
	public ValueTask<Guid?> DistributePromotionBonusAsync(
		[FromServices] IGetService<CreatePromotionDistributeQuery,Guid?> getService,
		[FromBody] DistributePromotionBonusViewModel request)
	{
		var command = new CreatePromotionDistributeQuery(request.BrandId, request.PromotionId, request.ConditionHash,
			request.PlayerCode, request.Amount, request.Distributor);

		return getService.GetAsync(command);
	}

	[HttpGet("{promotionId}/{conditionHash}")]
	[Description("Get distribute plan info")]
	public ValueTask<PromotionDistributePlanInfo?> GetDistributePlanInfoAsync(
		[FromServices] IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo?> getService,
		Guid promotionId,
		long conditionHash)
	{
		var command = new GetPromotionDistributePlanQuery(promotionId, conditionHash);

		return getService.GetAsync(command);
	}
}