using Core.Domain.Devices;
using MediatR;

namespace Application.Commands.Devices.DomainEventHandlers;

internal sealed class AutomationEvaluationDomainEventHandler
    : INotificationHandler<DeviceCapabilityStateUpdatedDomainEvent>
{
    private readonly IAutomationEvaluationQueue _queue;

    public AutomationEvaluationDomainEventHandler(IAutomationEvaluationQueue queue)
    {
        _queue = queue;
    }

    public async Task Handle(
        DeviceCapabilityStateUpdatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await _queue.EnqueueAsync(
            new AutomationEvaluationWorkItem(
                notification.DeviceId,
                notification.EndpointId,
                notification.CapabilityId,
                notification.State,
                notification.ReportedAt),
            cancellationToken);
    }
}
