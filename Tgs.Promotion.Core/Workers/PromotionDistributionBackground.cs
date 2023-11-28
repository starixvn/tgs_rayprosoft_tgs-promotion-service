using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Workers;

public class PromotionDistributionBackground : BackgroundService
{
	private readonly IPromotionDistributionTaskQueue _taskQueue;
	private readonly ILogger<PromotionDistributionBackground> _logger;
	private readonly IServiceProvider _serviceProvider;

	public PromotionDistributionBackground(IPromotionDistributionTaskQueue taskQueue, ILogger<PromotionDistributionBackground> logger, IServiceProvider serviceProvider)
	{
		_taskQueue = taskQueue;
		_logger = logger;
		_serviceProvider = serviceProvider;
	}

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Queued Hosted Service is running.{0}" +
            "{1}Tap W to add a work item to the " +
            "background queue.{2}",
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

	            var messageResponse = scope.ServiceProvider.GetRequiredService<ICommandService<DistributePromotionBonusManuallyCommand>>();

	            await messageResponse.ExecuteAsync(
		            new DistributePromotionBonusManuallyCommand(model.PromotionDistributionId,
			            model.PlayerCode,
			            model.Distributor),
		            stoppingToken);
            }
			catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }
}