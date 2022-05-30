using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Service.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Service.Controllers;

[Route("api/distribute")]
[ApiController]
public class PromotionDistributeController : ControllerBase
{
	/// <summary>
	/// Distribute the bonus of promotion
	/// </summary>
	/// <param name="getService">The get service.</param>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpPut("{planId}/{playerCode}")]
	public ValueTask<Guid> DistributePromotionBonusAsync(
		[FromServices] IGetService<DistributePromotionBonusCommand, Guid> getService,
		Guid planId,
		string playerCode,
		[FromBody] DistributePromotionBonusViewModel request)
	{
		var command = new DistributePromotionBonusCommand(
			planId,
			playerCode,
			request.Amount,
			request.NeedTurnover,
			request.Distributor);

		return getService.GetAsync(command);
	}

	/// <summary>
	/// Get or create distribute plan info
	/// </summary>
	/// <param name="getService">The get service.</param>
	/// <param name="promotionId">The promotion identifier.</param>
	/// <param name="conditionHash">The condition hash.</param>
	/// <returns></returns>
	[HttpPut("plan/{promotionId}/{conditionHash}")]
	public ValueTask<PromotionDistributePlanInfo?> GetDistributePlanInfoAsync(
		[FromServices] IGetService<GetOrCreatePromotionDistributePlanCommand, PromotionDistributePlanInfo?> getService,
		Guid promotionId,
		long conditionHash)
	{
		var command = new GetOrCreatePromotionDistributePlanCommand(promotionId, conditionHash);

		return getService.GetAsync(command);
	}
}
