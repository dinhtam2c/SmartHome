using Core.Domain.Homes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class HomeConfiguration : IEntityTypeConfiguration<Home>
{
    public void Configure(EntityTypeBuilder<Home> builder)
    {
        builder.ToTable("Homes");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.Name)
            .HasMaxLength(50);

        builder.Property(h => h.Description)
            .HasMaxLength(255);

        builder.OwnsMany(
            h => h.Rooms,
            l =>
            {
                l.ToTable("Rooms");

                l.HasKey(x => x.Id);
                l.Property(x => x.Id)
                    .ValueGeneratedNever();

                l.WithOwner()
                    .HasForeignKey(x => x.HomeId);

                l.Property(x => x.Name)
                    .HasMaxLength(50);

                l.Property(x => x.Description)
                    .HasMaxLength(255);

                l.HasIndex(x => x.HomeId);
            });
    }
}
