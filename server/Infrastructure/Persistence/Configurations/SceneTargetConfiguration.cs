using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SceneTargetConfiguration : IEntityTypeConfiguration<SceneTarget>
{
    public void Configure(EntityTypeBuilder<SceneTarget> builder)
    {
        builder.ToTable("SceneTargets");

        builder.HasKey(target => target.Id);
        builder.Property(target => target.Id)
            .ValueGeneratedNever();

        builder.Property(target => target.EndpointId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(target => target.CapabilityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(target => target.DesiredStatePayload)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(target => new { target.SceneId, target.Order });
        builder.HasIndex(target => new { target.DeviceId, target.CapabilityId, target.EndpointId });
    }
}
