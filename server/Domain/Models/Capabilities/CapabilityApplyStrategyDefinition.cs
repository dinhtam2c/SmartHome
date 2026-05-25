namespace Domain.Models.Capabilities;

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
