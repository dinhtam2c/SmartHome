using Domain.Models.Homes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Homes;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(room => room.Id);
        builder.Property(room => room.Id).ValueGeneratedNever();
        builder.Property(room => room.HomeId).IsRequired();
        builder.Property(room => room.Name).HasMaxLength(50).IsRequired();
        builder.Property(room => room.Description).HasMaxLength(255);
        builder.Property(room => room.CreatedAt).IsRequired();
        builder.HasIndex(room => room.HomeId);
    }
}
