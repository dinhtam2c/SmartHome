namespace Domain.Models.Capabilities;

public sealed class CapabilityPrerequisiteDefinition
{
    private readonly Dictionary<string, object?> _requiredState;

    public string CapabilityId { get; }
    public IReadOnlyDictionary<string, object?> RequiredState => _requiredState;
    public bool AutoFix { get; }

    public CapabilityPrerequisiteDefinition(
        string capabilityId,
        IReadOnlyDictionary<string, object?> requiredState,
        bool autoFix)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("Prerequisite capabilityId is required.", nameof(capabilityId));

        if (requiredState is null || requiredState.Count == 0)
            throw new ArgumentException("Prerequisite requiredState is required.", nameof(requiredState));

        CapabilityId = capabilityId.Trim();
        _requiredState = requiredState.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        AutoFix = autoFix;
    }
}
