using Domain.Models.ActionSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal sealed class ActionSetExecutionConfiguration : IEntityTypeConfiguration<ActionSetExecution>
{
    public void Configure(EntityTypeBuilder<ActionSetExecution> builder)
    {
        builder.ToTable("ActionSetExecutions");

        builder.HasKey(execution => execution.Id);
        builder.Property(execution => execution.Id).ValueGeneratedNever();
        builder.Property(execution => execution.SourceType).HasMaxLength(40).HasConversion<string>();
        builder.Property(execution => execution.Status).HasMaxLength(40).HasConversion<string>();
        builder.Property(execution => execution.Phase).HasMaxLength(40).HasConversion<string>();
        builder.Property(execution => execution.ExecutionMode).HasMaxLength(40).HasConversion<string>();

        builder.HasIndex(execution => execution.ActionSetId);
        builder.HasIndex(execution => new { execution.SourceType, execution.SourceId, execution.StartedAt });
        builder.HasIndex(execution => execution.HomeId);

        builder.HasMany(execution => execution.Actions)
            .WithOne()
            .HasForeignKey(action => action.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
