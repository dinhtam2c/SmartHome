using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SceneExecutionConfiguration : IEntityTypeConfiguration<SceneExecution>
{
    public void Configure(EntityTypeBuilder<SceneExecution> builder)
    {
        builder.ToTable("SceneExecutions");

        builder.HasKey(execution => execution.Id);
        builder.Property(execution => execution.Id)
            .ValueGeneratedNever();

        builder.Property(execution => execution.TriggerSource)
            .HasMaxLength(100);

        // Keep existing DB column names for backward compatibility while exposing target-first domain names.
        builder.Property(execution => execution.TotalTargets).HasColumnName("TotalTargets");
        builder.Property(execution => execution.PendingTargets).HasColumnName("PendingTargets");
        builder.Property(execution => execution.SkippedTargets).HasColumnName("SkippedTargets");
        builder.Property(execution => execution.SuccessfulTargets).HasColumnName("SuccessfulTargets");
        builder.Property(execution => execution.FailedTargets).HasColumnName("FailedTargets");

        builder.HasIndex(execution => new { execution.SceneId, execution.StartedAt });

        builder.HasMany(execution => execution.Targets)
            .WithOne()
            .HasForeignKey(target => target.SceneExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(execution => execution.SideEffects)
            .WithOne()
            .HasForeignKey(sideEffect => sideEffect.SceneExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
