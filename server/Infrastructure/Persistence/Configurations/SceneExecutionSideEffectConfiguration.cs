using Core.Domain.Scenes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class SceneExecutionSideEffectConfiguration : IEntityTypeConfiguration<SceneExecutionSideEffect>
{
    public void Configure(EntityTypeBuilder<SceneExecutionSideEffect> builder)
    {
        builder.ToTable("SceneExecutionSideEffects");

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

        builder.Property(sideEffect => sideEffect.CommandCorrelationId)
            .HasMaxLength(100);

        builder.Property(sideEffect => sideEffect.Error)
            .HasMaxLength(1000);

        builder.HasIndex(sideEffect => new { sideEffect.SceneExecutionId, sideEffect.Order });
        builder.HasIndex(sideEffect => new { sideEffect.DeviceId, sideEffect.CommandCorrelationId });
    }
}
