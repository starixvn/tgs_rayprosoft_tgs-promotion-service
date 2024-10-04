using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models.PromotionDistribute;
using Tgs.Promotion.Service.ViewModels;
using Tgs.Promotion.Workers;
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
			request.ReferenceId,
			request.Distributor,
			request.BucketTypes);

		return getService.GetAsync(command);
	}

	[HttpPut("manually")]
	public async ValueTask DistributePromotionBonusAsync(
			[FromServices] IPromotionDistributionTaskQueue taskQueue,
			[FromBody] DistributePromotionBonusManuallyViewModel request)
	{
		foreach (var player in request.Players)
		{
			var command = new PromotionDistributionQueueModel
			{
				PromotionDistributionId = player.PromotionBonusId,
				PlayerCode = player.PlayerCode,
				Distributor = request.Distributor
			};

			await taskQueue.QueuePromotionDistributeTaskQueueAsync(command);
		}
	}

	/// <summary>
	/// Get or create distribute plan info
	/// </summary>
	/// <param name="getService">The get service.</param>
	/// <param name="promotionId">The promotion identifier.</param>
	/// <param name="conditionHash">The condition hash.</param>
	/// <returns></returns>
	[HttpPut("plan/{promotionId}/{conditionHash}")]
	public ValueTask<PromotionDistributePlanInfo> GetDistributePlanInfoAsync(
		[FromServices] IGetService<GetOrCreatePromotionDistributePlanCommand, PromotionDistributePlanInfo> getService,
		Guid promotionId,
		long conditionHash)
	{
		var command = new GetOrCreatePromotionDistributePlanCommand(promotionId, conditionHash);

		return getService.GetAsync(command);
	}

	[HttpGet("{id}")]
	[Description("Get promotion distribution")]
	public ValueTask<PromotionDistributeModel> GetPromotionDistributionAsync(
		[FromServices] IGetService<GetPromotionDistributeQuery, PromotionDistributeModel> getService,
		Guid id) =>
		getService.GetAsync(new GetPromotionDistributeQuery(id));

	[HttpGet("detail/{promotionId}/{referenceId}")]
	[Description("Get promotion distribution detail")]
	public ValueTask<PromotionDistributeDetailModel> GetPromotionDistributionDetailAsync(
		[FromServices] IGetService<GetPromotionDistributeDetailQuery, PromotionDistributeDetailModel> getService,
		Guid promotionId,
		Guid referenceId) =>
		getService.GetAsync(new GetPromotionDistributeDetailQuery(promotionId, referenceId));
}
