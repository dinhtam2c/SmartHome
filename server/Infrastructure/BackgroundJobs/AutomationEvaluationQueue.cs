using System.Threading.Channels;
using Application.BusinessServices.Automations.Evaluation;

namespace Infrastructure.BackgroundJobs;

public sealed class AutomationEvaluationQueue : IAutomationEvaluationQueue
{
    private const int Capacity = 4096;

    private readonly Channel<AutomationEvaluationWorkItem> _channel =
        Channel.CreateBounded<AutomationEvaluationWorkItem>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(AutomationEvaluationWorkItem workItem, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(workItem, cancellationToken);
    }

    public IAsyncEnumerable<AutomationEvaluationWorkItem> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
