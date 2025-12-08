using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.ToTable("Sensors");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .HasMaxLength(50);
        builder.Property(s => s.Type)
            .HasMaxLength(50);
        builder.Property(s => s.Unit)
            .HasMaxLength(10);
        builder.Property(s => s.Min)
            .HasPrecision(6, 1);
        builder.Property(s => s.Max)
            .HasPrecision(6, 1);
        builder.Property(s => s.Accuracy)
            .HasPrecision(3, 2);

        // TODO: Add check constraints

        builder.HasOne(s => s.Device)
            .WithMany(d => d.Sensors)
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
