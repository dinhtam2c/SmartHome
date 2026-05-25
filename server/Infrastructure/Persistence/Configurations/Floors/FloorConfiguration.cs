using Domain.Models.Devices;
using Domain.Models.Floors;
using Domain.Models.Homes;
using Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Floors;

internal sealed class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.ToTable("Floors");
        builder.HasKey(floor => floor.Id);
        builder.Property(floor => floor.Id).ValueGeneratedNever();
        builder.Property(floor => floor.Name).HasMaxLength(100).IsRequired();
        builder.Property(floor => floor.HomeId).IsRequired();
        builder.Property(floor => floor.SortOrder).IsRequired();
        builder.Property(floor => floor.CanvasWidth).IsRequired();
        builder.Property(floor => floor.CanvasHeight).IsRequired();
        builder.Property(floor => floor.CreatedAt).IsRequired();
        builder.Property(floor => floor.UpdatedAt).IsRequired();

        builder.HasOne<Home>()
            .WithMany()
            .HasForeignKey(floor => floor.HomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(floor => new { floor.HomeId, floor.SortOrder }).IsUnique();
        builder.HasIndex(floor => floor.HomeId);

        builder.Navigation(floor => floor.FloorPlanRooms)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(floor => floor.DevicePlacements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(
            floor => floor.FloorPlanRooms,
            room =>
            {
                room.ToTable("FloorPlanRooms");
                room.WithOwner().HasForeignKey(item => item.FloorId);
                room.HasKey(item => item.Id);
                room.Property(item => item.Id).ValueGeneratedNever();
                room.Property(item => item.RoomId).IsRequired();
                room.Property(item => item.Polygon)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        value => JsonColumnSerializer.Serialize(value),
                        value => JsonColumnSerializer.DeserializeList<FloorPoint>(value))
                    .Metadata.SetValueComparer(
                        JsonColumnSerializer.CreateListComparer<FloorPoint>());
                room.Property(item => item.FillColor).HasMaxLength(20);
                room.HasIndex(item => item.RoomId).IsUnique();
                room.HasIndex(item => item.FloorId);
                room.HasOne<Room>()
                    .WithOne()
                    .HasForeignKey<FloorPlanRoom>(item => item.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        builder.OwnsMany(
            floor => floor.DevicePlacements,
            placement =>
            {
                placement.ToTable("FloorDevicePlacements");
                placement.WithOwner().HasForeignKey(item => item.FloorId);
                placement.HasKey(item => item.Id);
                placement.Property(item => item.Id).ValueGeneratedNever();
                placement.Property(item => item.DeviceId).IsRequired();
                placement.Property(item => item.X).IsRequired();
                placement.Property(item => item.Y).IsRequired();
                placement.HasIndex(item => item.DeviceId).IsUnique();
                placement.HasOne<Device>()
                    .WithOne()
                    .HasForeignKey<FloorDevicePlacement>(item => item.DeviceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
    }
}
