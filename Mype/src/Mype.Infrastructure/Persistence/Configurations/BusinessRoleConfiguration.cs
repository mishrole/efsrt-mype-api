using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.BusinessRoles;
using Mype.Domain.BusinessRoles.Constants;
using Mype.Domain.BusinessRoles.Constraints;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class BusinessRoleConfiguration
        : IEntityTypeConfiguration<BusinessRole>
    {
        public void Configure(
            EntityTypeBuilder<BusinessRole> builder
        )
        {
            builder.ToTable("business_roles");

            builder.HasKey(role => role.Id);

            builder.Property(role => role.Id)
                .ValueGeneratedNever();

            builder.Property(role => role.Code)
                .HasMaxLength(
                    BusinessRoleConstraints.CodeMaxLength
                )
                .IsRequired();

            builder.HasIndex(role => role.Code)
                .IsUnique()
                .HasDatabaseName(
                    DatabaseConstraints.BusinessRoles.Code
                );

            builder.Property(role => role.Name)
                .HasMaxLength(
                    BusinessRoleConstraints.NameMaxLength
                )
                .IsRequired();

            builder.Property(role => role.Description)
                .HasMaxLength(
                    BusinessRoleConstraints.DescriptionMaxLength
                )
                .IsRequired();

            builder.Property(role => role.IsSystem)
                .IsRequired();

            builder.Property(role => role.IsActive)
                .IsRequired();

            builder.Property(role => role.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(role => role.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasData(
                new
                {
                    Id = BusinessRoles.OwnerId,
                    Code = BusinessRoles.OwnerCode,
                    Name = "Propietario",
                    Description =
                        "Administra el negocio y sus miembros.",
                    IsSystem = true,
                    IsActive = true,
                    CreatedAt = BusinessRoles.SeededAt,
                    UpdatedAt = BusinessRoles.SeededAt
                },
                new
                {
                    Id = BusinessRoles.CollaboratorId,
                    Code = BusinessRoles.CollaboratorCode,
                    Name = "Colaborador",
                    Description =
                        "Participa en las operaciones del negocio.",
                    IsSystem = true,
                    IsActive = true,
                    CreatedAt = BusinessRoles.SeededAt,
                    UpdatedAt = BusinessRoles.SeededAt
                }
            );
        }
    }
}