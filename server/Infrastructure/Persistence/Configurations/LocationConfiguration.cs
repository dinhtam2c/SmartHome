using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .HasMaxLength(50);

        builder.HasOne(l => l.Home)
            .WithMany(h => h.Locations)
            .HasForeignKey(l => l.HomeId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
