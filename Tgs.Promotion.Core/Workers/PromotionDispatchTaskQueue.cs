using System.Threading.Channels;

namespace Tgs.Promotion.Workers;

public interface IPromotionDispatchTaskQueue
{
    ValueTask QueuePromotionDispatchTaskQueueAsync(PromotionDispatchQueueModel model);
    ValueTask<PromotionDispatchQueueModel> DequeueAsync(
        CancellationToken cancellationToken);
}

public class PromotionDispatchTaskQueue : IPromotionDispatchTaskQueue
{
    private readonly Channel<PromotionDispatchQueueModel> _queue = Channel
        .CreateUnbounded<PromotionDispatchQueueModel>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask QueuePromotionDispatchTaskQueueAsync(
	    PromotionDispatchQueueModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await _queue.Writer.WriteAsync(model);
    }
    public async ValueTask<PromotionDispatchQueueModel> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}