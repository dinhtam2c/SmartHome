using Core.Domain.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Automations;

class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> builder)
    {
        builder.ToTable("AutomationExecutions");

        builder.HasKey(execution => execution.Id);
        builder.Property(execution => execution.Id)
            .ValueGeneratedNever();

        builder.Property(execution => execution.TriggerEndpointId)
            .HasMaxLength(100);

        builder.Property(execution => execution.TriggerCapabilityId)
            .HasMaxLength(100);

        builder.Property(execution => execution.TriggerStatePayload)
            .HasMaxLength(4000);

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

        builder.HasIndex(execution => new { execution.RuleId, execution.StartedAt });
        builder.HasIndex(execution => execution.HomeId);

        builder.HasMany(execution => execution.Actions)
            .WithOne()
            .HasForeignKey(action => action.AutomationExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
