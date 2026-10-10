using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.BusinessRoles;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class BusinessMembershipConfiguration : IEntityTypeConfiguration<BusinessMembership>
    {
        public void Configure(EntityTypeBuilder<BusinessMembership> builder)
        {
            builder.ToTable("business_memberships");

            builder.HasKey(membership => membership.Id);

            builder.Property(membership => membership.Id).ValueGeneratedNever();

            builder.Property(membership => membership.BusinessId).IsRequired();

            builder.Property(membership => membership.UserId).IsRequired();

            builder.Property(membership => membership.RoleId).IsRequired();

            builder
                .HasIndex(membership => new { membership.BusinessId, membership.UserId })
                .IsUnique()
                .HasDatabaseName(DatabaseConstraints.BusinessMemberships.BusinessUser);

            builder.HasIndex(membership => new { membership.UserId, membership.Status });

            builder.HasIndex(membership => new { membership.BusinessId, membership.Status });

            builder.HasIndex(membership => membership.RoleId);

            builder
                .HasOne<Business>()
                .WithMany()
                .HasForeignKey(membership => membership.BusinessId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessMemberships.Business);

            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessMemberships.User);

            builder
                .HasOne<BusinessRole>()
                .WithMany()
                .HasForeignKey(membership => membership.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessMemberships.Role);

            builder.Property(membership => membership.Status).IsRequired();

            builder
                .Property(membership => membership.JoinedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder
                .Property(membership => membership.DeactivatedAt)
                .HasColumnType("timestamp with time zone");

            builder
                .Property(membership => membership.ReactivatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(membership => membership.CreatedByUserId).IsRequired();

            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(membership => membership.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessMemberships.CreatedByUser);

            builder.Property(membership => membership.UpdatedByUserId).IsRequired();

            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(membership => membership.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessMemberships.UpdatedByUser);

            builder
                .Property(membership => membership.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder
                .Property(membership => membership.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(membership => membership.Version).IsRowVersion();
        }
    }
}
