using Core.Domain.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Automations;

class AutomationActionConfiguration : IEntityTypeConfiguration<AutomationAction>
{
    public void Configure(EntityTypeBuilder<AutomationAction> builder)
    {
        builder.ToTable("AutomationActions");

        builder.HasKey(action => action.Id);
        builder.Property(action => action.Id)
            .ValueGeneratedNever();

        builder.Property(action => action.Section)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(action => action.Type)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(action => action.EndpointId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(action => action.CapabilityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(action => action.Operation)
            .HasMaxLength(100);

        builder.Property(action => action.StatePayload)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(action => action.OptionsPayload)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(action => action.Payload)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(action => new { action.RuleId, action.Section, action.Order });
        builder.HasIndex(action => new { action.DeviceId, action.CapabilityId, action.EndpointId });
    }
}

