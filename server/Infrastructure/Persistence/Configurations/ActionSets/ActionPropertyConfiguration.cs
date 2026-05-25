using Domain.Models.ActionSets;
using Infrastructure.Persistence.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.ActionSets;

internal static class ActionPropertyConfiguration
{
    public static void ConfigureActionSetActionProperties(this EntityTypeBuilder<ActionSetAction> builder)
    {
        builder.HasKey(action => action.Id);
        builder.Property(action => action.Id).ValueGeneratedNever();
        builder.Property(action => action.Section).HasMaxLength(40).HasConversion<string>();
        builder.Property(action => action.Type).HasMaxLength(40).HasConversion<string>();
        builder.Property(action => action.EndpointId).HasMaxLength(100).IsRequired();
        builder.Property(action => action.CapabilityId).HasMaxLength(100).IsRequired();
        builder.Property(action => action.Operation).HasMaxLength(100);

        builder.Property(action => action.State)
            .HasColumnName("StatePayload")
            .HasMaxLength(4000)
            .HasConversion(
                value => JsonColumnSerializer.Serialize(value),
                value => JsonColumnSerializer.DeserializeDictionary(value))
            .Metadata.SetValueComparer(JsonColumnSerializer.CreateDictionaryComparer());

        builder.Property(action => action.Payload)
            .HasColumnName("Payload")
            .HasMaxLength(4000)
            .HasConversion(
                value => JsonColumnSerializer.Serialize(value),
                value => JsonColumnSerializer.DeserializeDictionary(value))
            .Metadata.SetValueComparer(JsonColumnSerializer.CreateDictionaryComparer());
    }

    public static void ConfigureActionSetActionExecutionProperties(
        this EntityTypeBuilder<ActionSetActionExecution> builder)
    {
        builder.HasKey(action => action.Id);
        builder.Property(action => action.Id).ValueGeneratedNever();
        builder.Property(action => action.Section).HasMaxLength(40).HasConversion<string>();
        builder.Property(action => action.Type).HasMaxLength(40).HasConversion<string>();
        builder.Property(action => action.EndpointId).HasMaxLength(100).IsRequired();
        builder.Property(action => action.CapabilityId).HasMaxLength(100).IsRequired();
        builder.Property(action => action.Operation).HasMaxLength(100);

        builder.Property(action => action.State)
            .HasColumnName("StatePayload")
            .HasMaxLength(4000)
            .HasConversion(
                value => JsonColumnSerializer.Serialize(value),
                value => JsonColumnSerializer.DeserializeDictionary(value))
            .Metadata.SetValueComparer(JsonColumnSerializer.CreateDictionaryComparer());

        builder.Property(action => action.Payload)
            .HasColumnName("Payload")
            .HasMaxLength(4000)
            .HasConversion(
                value => JsonColumnSerializer.Serialize(value),
                value => JsonColumnSerializer.DeserializeDictionary(value))
            .Metadata.SetValueComparer(JsonColumnSerializer.CreateDictionaryComparer());
    }
}
