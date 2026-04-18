using System.Text.Json;
using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

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

                        var operationsComparer = new ValueComparer<IEnumerable<string>>(
                            (c1, c2) =>
                                (c1 == null && c2 == null)
                                || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                            c => c.ToList()
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
                            (c1, c2) =>
                                (c1 == null && c2 == null)
                                || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                            c =>
                                c.Aggregate(
                                    0,
                                    (a, v) =>
                                        HashCode.Combine(a, v.Key.GetHashCode(), v.Value != null ? v.Value.GetHashCode() : 0)),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
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
}
