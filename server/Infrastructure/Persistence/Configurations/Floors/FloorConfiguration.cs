using Core.Domain.Floors;
using Core.Domain.Homes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Floors;

internal sealed class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.ToTable("Floors");

        builder.HasKey(floor => floor.Id);
        builder.Property(floor => floor.Id)
            .ValueGeneratedNever();

        builder.Property(floor => floor.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(floor => floor.HomeId)
            .IsRequired();

        builder.Property(floor => floor.SortOrder)
            .IsRequired();

        builder.Property(floor => floor.CanvasWidth)
            .IsRequired();

        builder.Property(floor => floor.CanvasHeight)
            .IsRequired();

        builder.Property(floor => floor.CreatedAt)
            .IsRequired();

        builder.Property(floor => floor.UpdatedAt)
            .IsRequired();

        builder.HasOne<Home>()
            .WithMany()
            .HasForeignKey(floor => floor.HomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(floor => new { floor.HomeId, floor.SortOrder })
            .IsUnique();

        builder.HasIndex(floor => floor.HomeId);

        builder.Navigation(floor => floor.Rooms)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(floor => floor.PlacedFloorDevices)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(
            floor => floor.Rooms,
            room =>
            {
                room.ToTable("FloorRooms");

                room.WithOwner()
                    .HasForeignKey(x => x.FloorId);

                room.HasKey(x => x.Id);
                room.Property(x => x.Id)
                    .ValueGeneratedNever();

                room.Property(x => x.Label)
                    .HasMaxLength(100)
                    .IsRequired();

                room.Property(x => x.PolygonPayload)
                    .HasColumnName("Polygon")
                    .HasColumnType("TEXT")
                    .IsRequired();

                room.Property(x => x.FillColor)
                    .HasMaxLength(20);

                room.Property(x => x.LinkedRoomId);

                room.HasIndex(x => x.FloorId);
                room.HasIndex(x => x.LinkedRoomId);
            });

        builder.OwnsMany(
            floor => floor.PlacedFloorDevices,
            placedFloorDevice =>
            {
                placedFloorDevice.ToTable("FloorPlacedDevices");

                placedFloorDevice.WithOwner()
                    .HasForeignKey(x => x.FloorId);

                placedFloorDevice.HasKey(x => x.Id);
                placedFloorDevice.Property(x => x.Id)
                    .ValueGeneratedNever();

                placedFloorDevice.Property(x => x.DeviceId)
                    .IsRequired();

                placedFloorDevice.Property(x => x.X)
                    .IsRequired();

                placedFloorDevice.Property(x => x.Y)
                    .IsRequired();

                placedFloorDevice.Property(x => x.FloorRoomId);

                placedFloorDevice.HasIndex(x => x.DeviceId)
                    .IsUnique();
            });
    }
}
