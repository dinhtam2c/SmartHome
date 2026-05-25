using Domain.Models.ActionSets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal sealed class ActionSetActionConfiguration : IEntityTypeConfiguration<ActionSetAction>
{
    public void Configure(EntityTypeBuilder<ActionSetAction> builder)
    {
        builder.ToTable("ActionSetActions");

        builder.ConfigureActionSetActionProperties();

        builder.HasIndex(action => new { action.ActionSetId, action.Section, action.Order });
        builder.HasIndex(action => new { action.DeviceId, action.CapabilityId, action.EndpointId });
    }
}
