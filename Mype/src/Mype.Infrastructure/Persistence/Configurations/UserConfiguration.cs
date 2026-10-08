using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Users;
using Mype.Domain.Users.Constraints;
using Mype.Shared.Constants;
using System;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
            .ValueGeneratedNever();

            builder.Property(x => x.Email)
            .HasMaxLength(UserConstraints.EmailMaxLength)
            .IsRequired();

            builder.Property(x => x.NormalizedEmail)
            .HasMaxLength(UserConstraints.EmailMaxLength)
            .IsRequired();

            builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName(DatabaseConstraints.UsersNormalizedEmail);

            builder.Property(x => x.PasswordHash)
            .HasMaxLength(UserConstraints.PasswordHashMaxLength)
            .IsRequired();

            builder.Property(x => x.DisplayName)
            .HasMaxLength(UserConstraints.DisplayNameMaxLength)
            .IsRequired();

            builder.Property(x => x.EmailVerified)
            .IsRequired();

            builder.Property(x => x.Status)
            .HasConversion(
            status => status.ToString().ToUpperInvariant(),
            value => Enum.Parse<UserStatus>(value, true)
            )
            .HasMaxLength(UserConstraints.StatusMaxLength)
            .IsRequired();

            builder.Property(x => x.DeactivatedAt)
            .HasColumnType("timestamp with time zone");

            builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

            builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

            builder.Property(x => x.Version)
            .IsRowVersion();
        }
    }
}
