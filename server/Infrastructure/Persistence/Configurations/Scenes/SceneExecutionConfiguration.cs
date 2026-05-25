using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Scenes;

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

        builder.Property(execution => execution.Status)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(execution => execution.Phase)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(execution => execution.ExecutionMode)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.HasIndex(execution => new { execution.SceneId, execution.StartedAt });
        builder.HasIndex(execution => execution.HomeId);

        builder.HasMany(execution => execution.Actions)
            .WithOne()
            .HasForeignKey(action => action.SceneExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
