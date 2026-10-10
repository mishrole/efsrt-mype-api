using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.BusinessRolePermissions;
using Mype.Domain.BusinessRoles;
using Mype.Domain.BusinessRoles.Constants;
using Mype.Domain.Permissions;
using Mype.Domain.Permissions.Constants;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class BusinessRolePermissionConfiguration
        : IEntityTypeConfiguration<BusinessRolePermission>
    {
        public void Configure(EntityTypeBuilder<BusinessRolePermission> builder)
        {
            builder.ToTable("business_role_permissions");

            builder.HasKey(relation => new { relation.BusinessRoleId, relation.PermissionId });

            builder.Property(relation => relation.BusinessRoleId).ValueGeneratedNever();

            builder.Property(relation => relation.PermissionId).ValueGeneratedNever();

            builder
                .HasOne<BusinessRole>()
                .WithMany()
                .HasForeignKey(relation => relation.BusinessRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessRolePermissions.BusinessRole);

            builder
                .HasOne<Permission>()
                .WithMany()
                .HasForeignKey(relation => relation.PermissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.BusinessRolePermissions.Permission);

            builder
                .HasIndex(relation => relation.PermissionId)
                .HasDatabaseName(DatabaseConstraints.BusinessRolePermissions.PermissionIndex);

            builder
                .Property(relation => relation.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasData(
                SystemPermissions.Owner.Select(permission => new
                {
                    BusinessRoleId = BusinessRoleConstants.OwnerId,
                    PermissionId = permission.Id,
                    CreatedAt = BusinessRoleConstants.SeededAt,
                })
            );
        }
    }
}
