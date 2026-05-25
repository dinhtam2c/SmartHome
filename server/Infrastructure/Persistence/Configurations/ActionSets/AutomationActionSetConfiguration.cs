using Domain.Models.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal sealed class AutomationActionSetConfiguration : IEntityTypeConfiguration<AutomationActionSet>
{
    public void Configure(EntityTypeBuilder<AutomationActionSet> builder)
    {
        builder.HasIndex(actionSet => actionSet.RuleId).IsUnique();
    }
}
