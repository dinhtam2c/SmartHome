using Domain.Models.ActionSets;
using Domain.Models.Devices.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal sealed class ActionSetActionExecutionConfiguration
    : IEntityTypeConfiguration<ActionSetActionExecution>
{
    public void Configure(EntityTypeBuilder<ActionSetActionExecution> builder)
    {
        builder.ToTable("ActionSetActionExecutions");

        builder.ConfigureActionSetActionExecutionProperties();
        builder.Property(action => action.Status).HasMaxLength(40).HasConversion<string>();
        builder.Property(action => action.Error).HasMaxLength(1000);

        builder.HasOne<DeviceCommandExecution>()
            .WithMany()
            .HasForeignKey(action => action.DeviceCommandExecutionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(action => new { action.ExecutionId, action.Section, action.Order });
        builder.HasIndex(action => action.DeviceCommandExecutionId).IsUnique();
    }
}
