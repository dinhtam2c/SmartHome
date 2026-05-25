using Core.Domain.Automations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Automations;

class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("AutomationRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id)
            .ValueGeneratedNever();

        builder.Property(rule => rule.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(rule => rule.Description)
            .HasMaxLength(255);

        builder.Property(rule => rule.ConditionLogic)
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(rule => rule.TimeWindowEnabled);
        builder.Property(rule => rule.TimeWindowStartMinute);
        builder.Property(rule => rule.TimeWindowEndMinute);
        builder.Property(rule => rule.TimeWindowDaysOfWeekMask);

        builder.Property(rule => rule.ExecutionMode)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.HasIndex(rule => rule.HomeId);
        builder.HasIndex(rule => new { rule.HomeId, rule.Name });

        builder.HasMany(rule => rule.Conditions)
            .WithOne()
            .HasForeignKey(condition => condition.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(rule => rule.Actions)
            .WithOne()
            .HasForeignKey(action => action.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
