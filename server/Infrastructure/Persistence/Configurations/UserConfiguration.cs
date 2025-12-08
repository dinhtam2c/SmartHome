using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Name)
            .HasMaxLength(50);
        builder.Property(u => u.Username)
            .HasMaxLength(50);
        builder.Property(u => u.PasswordHash)
            .HasMaxLength(200);
        builder.Property(u => u.Role)
            .HasMaxLength(20);
        builder.Property(u => u.Email)
            .HasMaxLength(100);
        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.HasIndex(u => u.Username)
            .IsUnique();
        builder.HasIndex(u => u.Email)
            .IsUnique();
        builder.HasIndex(u => u.Phone)
            .IsUnique();
    }
}
