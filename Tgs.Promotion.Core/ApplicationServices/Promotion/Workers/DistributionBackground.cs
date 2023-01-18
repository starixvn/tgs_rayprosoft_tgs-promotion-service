using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tgs.Promotion.ApplicationServices.Promotion.Workers;

public class DistributionBackground : BackgroundService
{
	private readonly ILogger<DistributionBackground> _logger;
	private readonly IServiceProvider _serviceProvider;

	public DistributionBackground(
		IServiceProvider serviceProvider,
		ILogger<DistributionBackground> logger)
	{
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
			"Promotion Distribution Queued Hosted Service is running.");

		await BackgroundProcessingAsync(stoppingToken);
	}

	private async Task BackgroundProcessingAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var processor = scope.ServiceProvider.GetRequiredService<IDistributionTaskQueueProcessor>();
				await processor.ProcessQueueAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred executing task work item.");
			}
	}
}
