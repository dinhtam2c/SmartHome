using Core.Domain.Data;
using Core.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class DeviceCapabilityStateHistoryConfiguration : IEntityTypeConfiguration<DeviceCapabilityStateHistory>
{
    public void Configure(EntityTypeBuilder<DeviceCapabilityStateHistory> builder)
    {
        builder.ToTable("DeviceCapabilityStateHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.CapabilityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.EndpointId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.StatePayload)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(h => new { h.DeviceId, h.CapabilityId, h.EndpointId, h.ReportedAt });

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(h => h.DeviceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
