using System.Text.Json;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

class ActuatorConfiguration : IEntityTypeConfiguration<Actuator>
{
    public void Configure(EntityTypeBuilder<Actuator> builder)
    {
        builder.ToTable("Actuators");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.Name)
            .HasMaxLength(50);

        builder.Property(a => a.Type)
            .HasMaxLength(50);


        var statesComparer = new ValueComparer<Dictionary<ActuatorState, object?>?>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null &&
                l.Count == r.Count &&
                l.All(kv =>
                    r.ContainsKey(kv.Key) &&
                    Equals(kv.Value, r[kv.Key]))),

            v =>
                v == null
                    ? 0
                    : v.OrderBy(e => e.Key)
                    .Aggregate(17, (h, e) =>
                        HashCode.Combine(h, e.Key, e.Value)),

            v =>
                v == null
                    ? null
                    : v.ToDictionary(e => e.Key, e => e.Value)
        );
        builder.Property(a => a.States)
            .HasConversion(new ValueConverter<Dictionary<ActuatorState, object?>?, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<ActuatorState, object?>>(v, (JsonSerializerOptions?)null)
                    ?? new()
            ))
            .Metadata.SetValueComparer(statesComparer);

        builder.HasOne(a => a.Device)
            .WithMany(d => d.Actuators)
            .HasForeignKey(a => a.DeviceId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
