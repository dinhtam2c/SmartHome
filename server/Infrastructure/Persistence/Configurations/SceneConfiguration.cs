using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SceneConfiguration : IEntityTypeConfiguration<Scene>
{
    public void Configure(EntityTypeBuilder<Scene> builder)
    {
        builder.ToTable("Scenes");

        builder.HasKey(scene => scene.Id);
        builder.Property(scene => scene.Id)
            .ValueGeneratedNever();

        builder.Property(scene => scene.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(scene => scene.Description)
            .HasMaxLength(255);

        builder.HasIndex(scene => scene.HomeId);
        builder.HasIndex(scene => new { scene.HomeId, scene.Name });

        builder.HasMany(scene => scene.Targets)
            .WithOne()
            .HasForeignKey(target => target.SceneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(scene => scene.SideEffects)
            .WithOne()
            .HasForeignKey(sideEffect => sideEffect.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
