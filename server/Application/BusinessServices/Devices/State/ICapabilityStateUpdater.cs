namespace Application.BusinessServices.Devices.State;

public interface ICapabilityStateUpdater
{
    Task<IReadOnlyList<CapabilityStateUpdate>> Apply(
        Guid deviceId,
        IReadOnlyCollection<CapabilityStateUpdate> stateChanges,
        CancellationToken cancellationToken);
}
