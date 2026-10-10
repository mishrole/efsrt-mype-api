using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Businesses;
using Mype.Domain.Categories;
using Mype.Domain.Products;
using Mype.Domain.Products.Constraints;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration :
        IEntityTypeConfiguration<Product>
    {
        public void Configure(
            EntityTypeBuilder<Product> builder
        )
        {
            builder.ToTable("products");

            builder.HasKey(product =>
                product.Id
            );

            builder.Property(product =>
                    product.Id
                )
                .ValueGeneratedNever();

            builder.Property(product =>
                    product.BusinessId
                )
                .IsRequired();

            builder.Property(product =>
                    product.CategoryId
                )
                .IsRequired();

            builder.Property(product =>
                    product.Name
                )
                .HasMaxLength(
                    ProductConstraints
                        .NameMaxLength
                )
                .IsRequired();

            builder.Property(product =>
                    product.NormalizedName
                )
                .HasMaxLength(
                    ProductConstraints
                        .NormalizedNameMaxLength
                )
                .IsRequired();

            builder.Property(product =>
                    product.SalePrice
                )
                .HasPrecision(
                    ProductConstraints
                        .AmountPrecision,
                    ProductConstraints
                        .AmountScale
                )
                .IsRequired();

            builder.Property(product =>
                    product.UnitCost
                )
                .HasPrecision(
                    ProductConstraints
                        .AmountPrecision,
                    ProductConstraints
                        .AmountScale
                )
                .IsRequired();

            builder.Property(product =>
                    product.IsActive
                )
                .IsRequired();

            builder.Property(product =>
                    product.CreatedByUserId
                )
                .IsRequired();

            builder.Property(product =>
                    product.UpdatedByUserId
                )
                .IsRequired();

            builder.Property(product =>
                    product.DeactivatedAt
                )
                .HasColumnType(
                    "timestamp with time zone"
                );

            builder.Property(product =>
                    product.CreatedAt
                )
                .HasColumnType(
                    "timestamp with time zone"
                )
                .IsRequired();

            builder.Property(product =>
                    product.UpdatedAt
                )
                .HasColumnType(
                    "timestamp with time zone"
                )
                .IsRequired();

            builder.Property(product =>
                    product.Version
                )
                .IsRowVersion();

            builder.HasIndex(product =>
                new
                {
                    product.BusinessId,
                    product.NormalizedName
                }
            )
                .IsUnique()
                .HasDatabaseName(
                    DatabaseConstraints.Products
                        .BusinessName
                );

            builder.HasIndex(product =>
                new
                {
                    product.BusinessId,
                    product.IsActive,
                    product.NormalizedName
                }
            )
                .HasDatabaseName(
                    DatabaseConstraints.Products
                        .BusinessActiveName
                );

            builder.HasIndex(product =>
                new
                {
                    product.BusinessId,
                    product.CategoryId,
                    product.IsActive
                }
            )
                .HasDatabaseName(
                    DatabaseConstraints.Products
                        .BusinessCategoryActive
                );

            builder.HasOne<Business>()
                .WithMany()
                .HasForeignKey(product =>
                    product.BusinessId
                )
                .OnDelete(
                    DeleteBehavior.Restrict
                )
                .HasConstraintName(
                    DatabaseConstraints.Products
                        .Business
                );

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(product =>
                    new
                    {
                        product.CategoryId,
                        product.BusinessId
                    }
                )
                .HasPrincipalKey(category =>
                    new
                    {
                        category.Id,
                        category.BusinessId
                    }
                )
                .OnDelete(
                    DeleteBehavior.Restrict
                )
                .HasConstraintName(
                    DatabaseConstraints.Products
                        .CategoryBusiness
                );

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(product =>
                    product.CreatedByUserId
                )
                .OnDelete(
                    DeleteBehavior.Restrict
                )
                .HasConstraintName(
                    DatabaseConstraints.Products
                        .CreatedByUser
                );

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(product =>
                    product.UpdatedByUserId
                )
                .OnDelete(
                    DeleteBehavior.Restrict
                )
                .HasConstraintName(
                    DatabaseConstraints.Products
                        .UpdatedByUser
                );
        }
    }
}