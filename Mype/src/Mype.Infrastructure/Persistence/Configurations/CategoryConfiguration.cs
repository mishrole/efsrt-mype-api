using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Businesses;
using Mype.Domain.Categories;
using Mype.Domain.Categories.Constraints;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration
        : IEntityTypeConfiguration<Category>
    {
        public void Configure(
            EntityTypeBuilder<Category> builder
        )
        {
            builder.ToTable("categories");

            builder.HasKey(category => category.Id);

            builder.Property(category => category.Id)
                .ValueGeneratedNever();

            builder.Property(category => category.BusinessId)
                .IsRequired();

            builder.Property(category => category.Type)
                .IsRequired();

            builder.Property(category => category.Name)
                .HasMaxLength(
                    CategoryConstraints.NameMaxLength
                )
                .IsRequired();

            builder.Property(category =>
                    category.NormalizedName
                )
                .HasMaxLength(
                    CategoryConstraints.NormalizedNameMaxLength
                )
                .IsRequired();

            builder.Property(category => category.IsDefault)
                .IsRequired();

            builder.Property(category => category.IsActive)
                .IsRequired();

            builder.HasIndex(category => new
            {
                category.BusinessId,
                category.Type,
                category.NormalizedName
            })
                .IsUnique()
                .HasDatabaseName(
                    DatabaseConstraints.Categories.BusinessTypeName
                );

            builder.HasAlternateKey(category =>
                new
                {
                    category.Id,
                    category.BusinessId
                }
            )
                .HasName(
                    DatabaseConstraints.Categories.IdBusiness
                );

            builder.HasIndex(category => new
            {
                category.BusinessId,
                category.Type,
                category.IsActive
            });

            builder.HasOne<Business>()
                .WithMany()
                .HasForeignKey(category =>
                    category.BusinessId
                )
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    DatabaseConstraints.Categories.Business
                );

            builder.Property(category =>
                category.CreatedByUserId
            ).IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(category =>
                    category.CreatedByUserId
                )
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    DatabaseConstraints.Categories.CreatedByUser
                );

            builder.Property(category =>
                category.UpdatedByUserId
            ).IsRequired();

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(category =>
                    category.UpdatedByUserId
                )
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    DatabaseConstraints.Categories.UpdatedByUser
                );

            builder.Property(category =>
                    category.DeactivatedAt
                )
                .HasColumnType(
                    "timestamp with time zone"
                );

            builder.Property(category => category.CreatedAt)
                .HasColumnType(
                    "timestamp with time zone"
                )
                .IsRequired();

            builder.Property(category => category.UpdatedAt)
                .HasColumnType(
                    "timestamp with time zone"
                )
                .IsRequired();

            builder.Property(category => category.Version)
                .IsRowVersion();
        }
    }
}