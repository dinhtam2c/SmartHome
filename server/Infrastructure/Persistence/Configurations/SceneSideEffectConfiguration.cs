using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SceneSideEffectConfiguration : IEntityTypeConfiguration<SceneSideEffect>
{
    public void Configure(EntityTypeBuilder<SceneSideEffect> builder)
    {
        builder.ToTable("SceneSideEffects");

        builder.HasKey(sideEffect => sideEffect.Id);
        builder.Property(sideEffect => sideEffect.Id)
            .ValueGeneratedNever();

        builder.Property(sideEffect => sideEffect.EndpointId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sideEffect => sideEffect.CapabilityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sideEffect => sideEffect.Operation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sideEffect => sideEffect.ParamsPayload)
            .HasMaxLength(4000)
            .IsRequired();

        builder.HasIndex(sideEffect => new { sideEffect.SceneId, sideEffect.Order });
        builder.HasIndex(sideEffect => new { sideEffect.DeviceId, sideEffect.CapabilityId, sideEffect.EndpointId });
    }
}
