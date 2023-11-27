using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tgs.Promotion.Workers;

public class PromotionDistributionBackground : BackgroundService
{
	private readonly IPromotionDistributionTaskQueue _taskQueue;
	private readonly ILogger<PromotionDistributionBackground> _logger;
	public PromotionDistributionBackground(IPromotionDistributionTaskQueue taskQueue, ILogger<PromotionDistributionBackground> logger)
	{
		_taskQueue = taskQueue;
		_logger = logger;
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
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }
}