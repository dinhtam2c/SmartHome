using Core.Domain.DeviceCommandExecutions;
using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class DeviceCommandExecutionConfiguration : IEntityTypeConfiguration<DeviceCommandExecution>
{
    public void Configure(EntityTypeBuilder<DeviceCommandExecution> builder)
    {
        builder.ToTable("DeviceCommandExecutions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.CapabilityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EndpointId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Operation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.RequestPayload)
            .HasMaxLength(4000);

        builder.Property(e => e.ResultPayload)
            .HasMaxLength(4000);

        builder.Property(e => e.Error)
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.DeviceId, e.CorrelationId })
            .IsUnique();

        builder.HasIndex(e => new { e.DeviceId, e.RequestedAt });

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
