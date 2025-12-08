using Core.Entities;
using Microsoft.EntityFrameworkCore;
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

        builder.Property(d => d.Identifier)
            .HasMaxLength(100);
        builder.Property(d => d.Name)
            .HasMaxLength(50);
        builder.Property(d => d.Manufacturer)
            .HasMaxLength(50);
        builder.Property(d => d.Model)
            .HasMaxLength(50);
        builder.Property(d => d.FirmwareVersion)
            .HasMaxLength(10);

        builder.HasIndex(d => d.Identifier)
            .IsUnique();

        builder.HasOne(d => d.Gateway)
            .WithMany(g => g.Devices)
            .HasForeignKey(d => d.GatewayId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Location)
            .WithMany(l => l.Devices)
            .HasForeignKey(d => d.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
