namespace Domain.Models.Capabilities;

public sealed class CapabilityDefinition
{
    private readonly Dictionary<string, CapabilityOperationDefinition> _operations;
    private readonly HashSet<string> _conflictsWith;

    public string Id { get; }
    public int Version { get; }
    public CapabilityRole Role { get; }
    public string StateSchema { get; }
    public IReadOnlyDictionary<string, CapabilityOperationDefinition> Operations => _operations;
    public IReadOnlyCollection<string> ConflictsWith => _conflictsWith;
    public CapabilityPrerequisiteDefinition? Prerequisite { get; }
    public CapabilityApplyStrategyDefinition? ApplyStrategy { get; }

    public CapabilityDefinition(
        string id,
        int version,
        CapabilityRole role,
        string stateSchema,
        IDictionary<string, CapabilityOperationDefinition> operations,
        IEnumerable<string>? conflictsWith = null,
        CapabilityPrerequisiteDefinition? prerequisite = null,
        CapabilityApplyStrategyDefinition? applyStrategy = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Capability id is required.", nameof(id));

        if (version <= 0)
            throw new ArgumentException("Capability version must be greater than 0.", nameof(version));

        if (role != CapabilityRole.Actuator && string.IsNullOrWhiteSpace(stateSchema))
            throw new ArgumentException("State schema is required.", nameof(stateSchema));

        if (role == CapabilityRole.Control && applyStrategy is null)
            throw new ArgumentException("Apply strategy is required for control capabilities.", nameof(applyStrategy));

        Id = id.Trim();
        Version = version;
        Role = role;
        StateSchema = string.IsNullOrWhiteSpace(stateSchema) ? "{}" : stateSchema;
        _operations = new Dictionary<string, CapabilityOperationDefinition>(
            operations,
            StringComparer.OrdinalIgnoreCase);
        _conflictsWith = conflictsWith?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
        Prerequisite = prerequisite;
        ApplyStrategy = applyStrategy;

        if (ApplyStrategy is not null && !_operations.ContainsKey(ApplyStrategy.Operation))
        {
            throw new ArgumentException(
                $"Apply strategy operation '{ApplyStrategy.Operation}' is not defined in operations.",
                nameof(applyStrategy));
        }
    }

    public bool SupportsOperation(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
            return false;

        return _operations.ContainsKey(operation.Trim());
    }

    public bool TryGetOperation(
        string operation,
        out CapabilityOperationDefinition definition)
    {
        definition = default!;

        if (string.IsNullOrWhiteSpace(operation))
            return false;

        return _operations.TryGetValue(operation.Trim(), out definition!);
    }

    public bool ConflictsWithCapability(string capabilityId)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            return false;

        return _conflictsWith.Contains(capabilityId.Trim());
    }

    public bool IsReadOnlyField(string field)
    {
        return ApplyStrategy?.ReadOnlyFields.Contains(field, StringComparer.OrdinalIgnoreCase) == true;
    }
}
