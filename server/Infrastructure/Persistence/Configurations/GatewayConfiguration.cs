using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class GatewayConfiguration : IEntityTypeConfiguration<Gateway>
{
    public void Configure(EntityTypeBuilder<Gateway> builder)
    {
        builder.ToTable("Gateways");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.Name)
            .HasMaxLength(50);
        builder.Property(g => g.Manufacturer)
            .HasMaxLength(50);
        builder.Property(g => g.Model)
            .HasMaxLength(50);
        builder.Property(g => g.FirmwareVersion)
            .HasMaxLength(10);
        builder.Property(g => g.Mac)
            .HasMaxLength(17);

        builder.HasIndex(g => g.Mac)
            .IsUnique();

        builder.HasOne(g => g.Home)
            .WithMany(h => h.Gateways)
            .HasForeignKey(g => g.HomeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
