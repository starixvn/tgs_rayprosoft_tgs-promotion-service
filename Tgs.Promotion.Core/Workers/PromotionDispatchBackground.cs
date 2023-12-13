using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Workers;

public class PromotionDispatchBackground(IPromotionDispatchTaskQueue taskQueue, ILogger<PromotionDispatchBackground> logger, IServiceProvider serviceProvider) : BackgroundService
{
	private readonly IPromotionDispatchTaskQueue _taskQueue = taskQueue;
	private readonly ILogger<PromotionDispatchBackground> _logger = logger;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	public override async Task StopAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Queued Hosted Service is stopping.");

		await base.StopAsync(stoppingToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"Queued Hosted Service is running.{newLine}"
				+ "{newLine}Tap W to add a work item to the "
				+ "background queue.{newLine}",
			Environment.NewLine,
			Environment.NewLine,
			Environment.NewLine);

		await BackgroundProcessingAsync(stoppingToken);
	}

	private async Task BackgroundProcessingAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var model = await _taskQueue.DequeueAsync(stoppingToken);

			try
			{
				using var scope = _serviceProvider.CreateScope();

				var messageResponse = scope.ServiceProvider.GetRequiredService<IGetService<DispatchPromotionBonusQuery, MessageResponse>>();

				_ = await messageResponse.GetAsync(new DispatchPromotionBonusQuery
				{
					OperatorUserId = model.OperatorUserId,
					Players = model.Players.Select(x => new DispatchPlayer
					{
						PlayerCode = x.PlayerCode,
						PromotionBonusId = x.PromotionBonusId
					}).ToArray()
				}, stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred executing task work item.");
			}
		}
	}
}