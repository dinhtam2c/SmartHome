using Domain.Models.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal sealed class SceneActionSetConfiguration : IEntityTypeConfiguration<SceneActionSet>
{
    public void Configure(EntityTypeBuilder<SceneActionSet> builder)
    {
        builder.HasIndex(actionSet => actionSet.SceneId).IsUnique();
    }
}
