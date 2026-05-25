using Domain.Models.ActionSets;
using Domain.Models.Automations;
using Domain.Models.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal sealed class ActionSetConfiguration : IEntityTypeConfiguration<ActionSet>
{
    public void Configure(EntityTypeBuilder<ActionSet> builder)
    {
        builder.ToTable("ActionSets");

        builder.HasKey(actionSet => actionSet.Id);
        builder.Property(actionSet => actionSet.Id).ValueGeneratedNever();

        builder.Property(actionSet => actionSet.ExecutionMode)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Ignore(actionSet => actionSet.BeforeActions);
        builder.Ignore(actionSet => actionSet.MainActions);
        builder.Ignore(actionSet => actionSet.OnSuccessActions);
        builder.Ignore(actionSet => actionSet.OnFailureActions);

        builder.HasDiscriminator<string>("ActionSetType")
            .HasValue<SceneActionSet>("Scene")
            .HasValue<AutomationActionSet>("Automation");

        builder.HasMany(actionSet => actionSet.Actions)
            .WithOne()
            .HasForeignKey(action => action.ActionSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(actionSet => actionSet.Actions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
