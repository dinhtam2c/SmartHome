namespace Domain.Models.Capabilities;

public sealed class CapabilityOperationDefinition
{
    public string Name { get; }
    public string CommandSchema { get; }

    public CapabilityOperationDefinition(
        string name,
        string commandSchema)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Operation name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(commandSchema))
            throw new ArgumentException("Operation command schema is required.", nameof(commandSchema));

        Name = name.Trim();
        CommandSchema = commandSchema;
    }
}
