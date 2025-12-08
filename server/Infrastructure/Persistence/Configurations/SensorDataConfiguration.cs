using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SensorDataConfiguration : IEntityTypeConfiguration<SensorData>
{
    public void Configure(EntityTypeBuilder<SensorData> builder)
    {
        builder.ToTable("SensorData");
        builder.HasKey(sd => sd.Id);
        builder.Property(sd => sd.Id)
            .ValueGeneratedNever();

        builder.Property(sd => sd.Location)
            .HasMaxLength(50);
        builder.Property(sd => sd.Unit)
            .HasMaxLength(10);
        builder.Property(sd => sd.Value)
            .HasPrecision(6, 1);

        builder.HasOne(sd => sd.Sensor)
            .WithMany()
            .HasForeignKey(sd => sd.SensorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
