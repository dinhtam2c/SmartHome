namespace Core.Domain.Devices;

public sealed class CapabilityDefinition
{
    private readonly Dictionary<string, CapabilityOperationDefinition> _operations;
    private readonly HashSet<string> _conflictsWith;

    public string Id { get; }
    public int Version { get; }
    public CapabilityRole Role { get; }
    public string StateSchema { get; }
    public string Metadata { get; }
    public IReadOnlyDictionary<string, CapabilityOperationDefinition> Operations => _operations;
    public IReadOnlyCollection<string> ConflictsWith => _conflictsWith;
    public CapabilityPrerequisiteDefinition? Prerequisite { get; }
    public CapabilityApplyStrategyDefinition? ApplyStrategy { get; }

    public CapabilityDefinition(
        string id,
        int version,
        CapabilityRole role,
        string stateSchema,
        string metadata,
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
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
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

public sealed class CapabilityOperationDefinition
{
    public string Name { get; }
    public string FullDefinitionSchema { get; }
    public string CommandSchema { get; }

    public CapabilityOperationDefinition(
        string name,
        string fullDefinitionSchema,
        string commandSchema)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Operation name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(fullDefinitionSchema))
            throw new ArgumentException(
                "Operation full definition schema is required.",
                nameof(fullDefinitionSchema));

        if (string.IsNullOrWhiteSpace(commandSchema))
            throw new ArgumentException("Operation command schema is required.", nameof(commandSchema));

        Name = name.Trim();
        FullDefinitionSchema = fullDefinitionSchema;
        CommandSchema = commandSchema;
    }
}

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

public sealed class CapabilityApplyStrategyDefinition
{
    private readonly Dictionary<string, string> _stateMapping;
    private readonly HashSet<string> _readOnlyFields;

    public string Operation { get; }
    public IReadOnlyDictionary<string, string> StateMapping => _stateMapping;
    public IReadOnlyCollection<string> ReadOnlyFields => _readOnlyFields;
    public bool PartialUpdate { get; }

    public CapabilityApplyStrategyDefinition(
        string operation,
        IReadOnlyDictionary<string, string> stateMapping,
        IEnumerable<string>? readOnlyFields = null,
        bool partialUpdate = true)
    {
        if (string.IsNullOrWhiteSpace(operation))
            throw new ArgumentException("Apply strategy operation is required.", nameof(operation));

        if (stateMapping is null || stateMapping.Count == 0)
            throw new ArgumentException("Apply strategy stateMapping is required.", nameof(stateMapping));

        Operation = operation.Trim();
        _stateMapping = stateMapping.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        _readOnlyFields = readOnlyFields?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
        PartialUpdate = partialUpdate;
    }
}
