using Domain.Models.Automations;
using Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Automations;

class AutomationConditionConfiguration : IEntityTypeConfiguration<AutomationCondition>
{
    public void Configure(EntityTypeBuilder<AutomationCondition> builder)
    {
        builder.ToTable("AutomationConditions");

        builder.HasKey(condition => condition.Id);
        builder.Property(condition => condition.Id)
            .ValueGeneratedNever();

        builder.Property(condition => condition.EndpointId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(condition => condition.CapabilityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(condition => condition.FieldPath)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(condition => condition.Operator)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(condition => condition.CompareValue)
            .HasColumnName("CompareValuePayload")
            .HasMaxLength(4000)
            .HasConversion(
                value => JsonColumnSerializer.Serialize(value),
                value => JsonColumnSerializer.DeserializeValue(value))
            .Metadata.SetValueComparer(JsonColumnSerializer.CreateValueComparer());

        builder.HasIndex(condition => new { condition.RuleId, condition.Order });
        builder.HasIndex(condition => new
        {
            condition.DeviceId,
            condition.EndpointId,
            condition.CapabilityId
        });
    }
}
