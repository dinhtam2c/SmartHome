using Domain.Models.Homes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Homes;

internal sealed class HomeConfiguration : IEntityTypeConfiguration<Home>
{
    public void Configure(EntityTypeBuilder<Home> builder)
    {
        builder.ToTable("Homes");
        builder.HasKey(home => home.Id);
        builder.Property(home => home.Id).ValueGeneratedNever();
        builder.Property(home => home.Name).HasMaxLength(50).IsRequired();
        builder.Property(home => home.Description).HasMaxLength(255);

        builder.HasMany(home => home.Rooms)
            .WithOne()
            .HasForeignKey(room => room.HomeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(home => home.Rooms)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
