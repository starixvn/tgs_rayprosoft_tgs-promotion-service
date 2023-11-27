using System.Threading.Channels;

namespace Tgs.Promotion.Workers;

public interface IPromotionDispatchTaskQueue
{
    ValueTask QueuePromotionDispatchTaskQueueAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken);
}

public class PromotionDispatchTaskQueue : IPromotionDispatchTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue = Channel
        .CreateUnbounded<Func<CancellationToken, ValueTask>>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask QueuePromotionDispatchTaskQueueAsync(
        Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        await _queue.Writer.WriteAsync(workItem);
    }
    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}