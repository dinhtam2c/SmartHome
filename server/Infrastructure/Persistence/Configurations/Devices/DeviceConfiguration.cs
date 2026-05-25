using System.Text.Json;
using Core.Common;
using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Devices;

class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.Name)
            .HasMaxLength(50);

        builder.Property(d => d.Category)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(d => d.MacAddress)
            .HasMaxLength(20);

        builder.Property(d => d.FirmwareVersion)
            .HasMaxLength(20);

        builder.Property(d => d.Protocol)
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(d => d.ProvisionState)
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(d => d.ProvisionCode)
            .HasMaxLength(6);

        builder.Property(d => d.AccessToken)
            .HasMaxLength(64);

        builder.HasIndex(d => d.MacAddress)
            .IsUnique();
        builder.HasIndex(d => d.ProvisionCode)
            .IsUnique();
        builder.HasIndex(d => d.AccessToken)
            .IsUnique();

        builder.HasIndex(d => d.HomeId);
        builder.HasIndex(d => d.RoomId);

        builder.Ignore(d => d.Capabilities);

        builder.OwnsMany(
            d => d.Endpoints,
            endpoint =>
            {
                endpoint.ToTable("DeviceEndpoints");

                endpoint.WithOwner().HasForeignKey(x => x.DeviceId);

                endpoint.HasKey(x => x.Id);
                endpoint.Property(x => x.Id)
                    .ValueGeneratedNever();

                endpoint.Property(x => x.EndpointId)
                    .HasMaxLength(100)
                    .IsRequired();

                endpoint.Property(x => x.Name)
                    .HasMaxLength(100);

                endpoint.HasIndex(x => new { x.DeviceId, x.EndpointId })
                    .IsUnique();

                endpoint.OwnsMany(
                    x => x.Capabilities,
                    capability =>
                    {
                        capability.ToTable("DeviceCapabilities");

                        capability.WithOwner().HasForeignKey(x => x.EndpointId);

                        capability.HasKey(x => x.Id);
                        capability.Property(x => x.Id)
                            .ValueGeneratedNever();

                        capability.Property(x => x.CapabilityId)
                            .HasMaxLength(100)
                            .IsRequired();

                        capability.Property(x => x.CapabilityVersion)
                            .IsRequired();

                        var operationsComparer = new ValueComparer<IEnumerable<string>?>(
                            (c1, c2) => NormalizeOperations(c1).SequenceEqual(
                                NormalizeOperations(c2),
                                StringComparer.OrdinalIgnoreCase),
                            c => NormalizeOperations(c).Aggregate(
                                0,
                                (hash, value) => HashCode.Combine(
                                    hash,
                                    StringComparer.OrdinalIgnoreCase.GetHashCode(value))),
                            c => NormalizeOperations(c).ToList()
                        );

                        capability.Property(x => x.SupportedOperations)
                            .HasConversion(
                                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                                v =>
                                    JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
                                    ?? new List<string>()
                            )
                            .Metadata.SetValueComparer(operationsComparer);

                        var stateComparer = new ValueComparer<IReadOnlyDictionary<string, object?>>(
                            (c1, c2) => StateDictionariesEqual(c1, c2),
                            c => GetStateHashCode(c),
                            c => SnapshotState(c)
                        );

                        capability.Property(x => x.State)
                            .HasConversion(
                                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                                v =>
                                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                        v,
                                        (JsonSerializerOptions?)null)
                                    ?? new Dictionary<string, object?>()
                            )
                            .Metadata.SetValueComparer(stateComparer);

                        capability.HasIndex(x => new { x.EndpointId, x.CapabilityId })
                            .IsUnique();

                        capability.Ignore(x => x.RuntimeHints);
                    });
            });
    }

    private static IReadOnlyList<string> NormalizeOperations(IEnumerable<string>? operations)
    {
        return operations?
            .Where(operation => !string.IsNullOrWhiteSpace(operation))
            .Select(operation => operation.Trim())
            .ToList()
            ?? [];
    }

    private static bool StateDictionariesEqual(
        IReadOnlyDictionary<string, object?>? left,
        IReadOnlyDictionary<string, object?>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null || left.Count != right.Count)
            return false;

        foreach (var entry in left)
        {
            var hasMatchingKey = false;
            object? rightValue = null;

            foreach (var rightEntry in right)
            {
                if (!string.Equals(entry.Key, rightEntry.Key, StringComparison.OrdinalIgnoreCase))
                    continue;

                hasMatchingKey = true;
                rightValue = rightEntry.Value;
                break;
            }

            if (!hasMatchingKey)
                return false;

            if (!StateValuesEqual(entry.Value, rightValue))
                return false;
        }

        return true;
    }

    private static bool StateValuesEqual(object? left, object? right)
    {
        return JsonSerializer.Serialize(NormalizeStateValue(left))
            == JsonSerializer.Serialize(NormalizeStateValue(right));
    }

    private static int GetStateHashCode(IReadOnlyDictionary<string, object?> state)
    {
        var hash = new HashCode();

        foreach (var entry in state.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(entry.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(JsonSerializer.Serialize(NormalizeStateValue(entry.Value)));
        }

        return hash.ToHashCode();
    }

    private static IReadOnlyDictionary<string, object?> SnapshotState(IReadOnlyDictionary<string, object?> state)
    {
        return state.ToDictionary(
            entry => entry.Key,
            entry => NormalizeStateValue(entry.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static object? NormalizeStateValue(object? value)
    {
        return value switch
        {
            JsonElement element => NormalizeStateValue(JsonPayloadHelper.ConvertJsonElement(element)),
            IReadOnlyDictionary<string, object?> map => map
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    entry => entry.Key,
                    entry => NormalizeStateValue(entry.Value),
                    StringComparer.OrdinalIgnoreCase),
            IEnumerable<object?> values => values.Select(NormalizeStateValue).ToList(),
            _ => value
        };
    }
}
