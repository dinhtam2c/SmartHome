using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SceneExecutionTargetConfiguration : IEntityTypeConfiguration<SceneExecutionTarget>
{
    public void Configure(EntityTypeBuilder<SceneExecutionTarget> builder)
    {
        builder.ToTable("SceneExecutionTargets");

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

        builder.Property(target => target.CommandCorrelationId)
            .HasMaxLength(100);

        builder.Property(target => target.UnresolvedDiffPayload)
            .HasMaxLength(4000);

        builder.Property(target => target.Error)
            .HasMaxLength(1000);

        builder.HasIndex(target => new { target.SceneExecutionId, target.Order });
        builder.HasIndex(target => new { target.DeviceId, target.CommandCorrelationId });
    }
}
