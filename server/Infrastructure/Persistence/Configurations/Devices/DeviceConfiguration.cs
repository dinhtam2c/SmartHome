using Domain.Models.Devices;
using Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Models.Homes;

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

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(device => device.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

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
                                value => JsonColumnSerializer.Serialize(value ?? Array.Empty<string>()),
                                value => JsonColumnSerializer.DeserializeList<string>(value)
                            )
                            .Metadata.SetValueComparer(operationsComparer);

                        capability.Property(x => x.State)
                            .HasConversion(
                                value => JsonColumnSerializer.Serialize(value),
                                value => JsonColumnSerializer.DeserializeDictionary(value)
                            )
                            .Metadata.SetValueComparer(JsonColumnSerializer.CreateDictionaryComparer());

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

}
