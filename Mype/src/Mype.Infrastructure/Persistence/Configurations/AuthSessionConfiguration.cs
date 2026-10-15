using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.AuthSessions;
using Mype.Domain.AuthSessions.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
    {
        public void Configure(EntityTypeBuilder<AuthSession> builder)
        {
            builder.ToTable("auth_sessions");
            builder.HasKey(session => session.Id);
            builder.Property(session => session.Id).ValueGeneratedNever();
            builder.Property(session => session.FamilyId).IsRequired();
            builder.Property(session => session.UserId).IsRequired();
            builder
                .Property(session => session.RefreshTokenVerifier)
                .HasMaxLength(AuthSessionConstraints.RefreshTokenVerifierMaxLength)
                .IsFixedLength()
                .IsRequired();
            builder
                .Property(session => session.CreatedAt)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(session => session.UpdatedAt)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(session => session.IdleExpiresAt)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(session => session.AbsoluteExpiresAt)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(session => session.RevokedAt)
                .HasColumnType("timestamp with time zone");
            builder.Property(session => session.RevocationReason);
            builder.Property(session => session.ReplacedBySessionId);
            builder.Property(session => session.Version).IsRowVersion();

            builder
                .HasIndex(session => session.RefreshTokenVerifier)
                .IsUnique()
                .HasDatabaseName("ux_auth_sessions_refresh_token_verifier");
            builder
                .HasIndex(session => new { session.UserId, session.RevokedAt })
                .HasDatabaseName("ix_auth_sessions_user_id_revoked_at");
            builder
                .HasIndex(session => session.FamilyId)
                .HasDatabaseName("ix_auth_sessions_family_id");
            builder
                .HasIndex(session => session.IdleExpiresAt)
                .HasDatabaseName("ix_auth_sessions_idle_expires_at");
            builder
                .HasIndex(session => session.AbsoluteExpiresAt)
                .HasDatabaseName("ix_auth_sessions_absolute_expires_at");

            builder
                .HasOne<AuthSession>()
                .WithMany()
                .HasForeignKey(session => session.ReplacedBySessionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder
                .HasOne<Mype.Domain.Users.User>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
