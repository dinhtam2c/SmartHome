using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Scenes;

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

        builder.Property(scene => scene.ExecutionMode)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.HasIndex(scene => scene.HomeId);
        builder.HasIndex(scene => new { scene.HomeId, scene.Name });

        builder.HasMany(scene => scene.Actions)
            .WithOne()
            .HasForeignKey(action => action.SceneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
