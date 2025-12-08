using System.Text.Json;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class ActuatorConfiguration : IEntityTypeConfiguration<Actuator>
{
    public void Configure(EntityTypeBuilder<Actuator> builder)
    {
        builder.ToTable("Actuators");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.Name)
            .HasMaxLength(50);

        builder.Property(a => a.Type)
            .HasMaxLength(50);

        builder.Ignore(a => a.States);

        builder.HasOne(a => a.Device)
            .WithMany(d => d.Actuators)
            .HasForeignKey(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
