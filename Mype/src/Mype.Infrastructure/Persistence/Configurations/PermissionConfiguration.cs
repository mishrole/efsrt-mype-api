using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.BusinessRoles.Constants;
using Mype.Domain.Permissions;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Permissions.Constraints;
using Mype.Infrastructure.Persistence.Constraints;
using System.Linq;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class PermissionConfiguration
        : IEntityTypeConfiguration<Permission>
    {
        public void Configure(
            EntityTypeBuilder<Permission> builder
        )
        {
            builder.ToTable("permissions");

            builder.HasKey(permission => permission.Id);

            builder.Property(permission => permission.Id)
                .ValueGeneratedNever();

            builder.Property(permission => permission.Code)
                .HasMaxLength(
                    PermissionConstraints.CodeMaxLength
                )
                .IsRequired();

            builder.HasIndex(permission => permission.Code)
                .IsUnique()
                .HasDatabaseName(
                    DatabaseConstraints.Permissions.Code
                );

            builder.Property(permission => permission.Name)
                .HasMaxLength(
                    PermissionConstraints.NameMaxLength
                )
                .IsRequired();

            builder.Property(
                    permission => permission.Description
                )
                .HasMaxLength(
                    PermissionConstraints
                        .DescriptionMaxLength
                )
                .IsRequired();

            builder.Property(
                    permission => permission.IsSystem
                )
                .IsRequired();

            builder.Property(
                    permission => permission.IsActive
                )
                .IsRequired();

            builder.Property(
                    permission => permission.CreatedAt
                )
                .HasColumnType(
                    "timestamp with time zone"
                )
                .IsRequired();

            builder.Property(
                    permission => permission.UpdatedAt
                )
                .HasColumnType(
                    "timestamp with time zone"
                )
                .IsRequired();

            builder.HasData(
                SystemPermissions.All
                    .Select(permission => new
                    {
                        permission.Id,
                        permission.Code,
                        permission.Name,
                        permission.Description,
                        IsSystem = true,
                        IsActive = true,
                        CreatedAt =
                            BusinessRoleConstants.SeededAt,
                        UpdatedAt =
                            BusinessRoleConstants.SeededAt
                    })
            );
        }
    }
}