using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.ProductCategories;
using Mype.Domain.Products;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public sealed class FinancialMovementItemConfiguration
        : IEntityTypeConfiguration<FinancialMovementItem>
    {
        public void Configure(EntityTypeBuilder<FinancialMovementItem> itemBuilder)
        {
            itemBuilder.ToTable(
                "financial_movement_items",
                tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.QuantityPositive,
                        "quantity > 0"
                    );
                    tableBuilder.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.UnitAmountNonNegative,
                        "unit_amount >= 0"
                    );
                    tableBuilder.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.SubtotalNonNegative,
                        "subtotal_amount >= 0"
                    );
                    tableBuilder.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.UnitCostNonNegative,
                        "unit_cost_snapshot IS NULL OR unit_cost_snapshot >= 0"
                    );
                    tableBuilder.HasCheckConstraint(
                        DatabaseConstraints
                            .FinancialMovementItems
                            .ProductCategorySnapshotConsistency,
                        """
                        (
                            product_id IS NOT NULL
                            AND product_category_id IS NOT NULL
                            AND unit_cost_snapshot IS NOT NULL
                        )
                        OR
                        (
                            product_id IS NULL
                            AND product_category_id IS NULL
                            AND unit_cost_snapshot IS NULL
                        )
                        """
                    );
                }
            );

            itemBuilder.HasKey(financialMovementItem => financialMovementItem.Id);
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.Id)
                .ValueGeneratedNever();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.Description)
                .HasMaxLength(FinancialMovementItemConstraints.DescriptionMaxLength)
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.Quantity)
                .HasPrecision(18, 4)
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.UnitAmount)
                .HasPrecision(18, 2)
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.UnitCostSnapshot)
                .HasPrecision(18, 2);
            itemBuilder.Property(financialMovementItem => financialMovementItem.ProductCategoryId);
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.SubtotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.IsActive)
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.RetiredAt)
                .HasColumnType("timestamp with time zone");
            itemBuilder
                .Property(financialMovementItem => financialMovementItem.Version)
                .IsRowVersion();

            itemBuilder
                .HasIndex(financialMovementItem => new
                {
                    financialMovementItem.MovementId,
                    financialMovementItem.IsActive,
                })
                .HasDatabaseName(DatabaseConstraints.FinancialMovementItems.MovementActive);
            itemBuilder
                .HasIndex(financialMovementItem => new
                {
                    financialMovementItem.BusinessId,
                    financialMovementItem.CategoryId,
                })
                .HasDatabaseName(DatabaseConstraints.FinancialMovementItems.BusinessCategory);
            itemBuilder
                .HasIndex(financialMovementItem => new
                {
                    financialMovementItem.BusinessId,
                    financialMovementItem.ProductId,
                })
                .HasFilter("product_id IS NOT NULL")
                .HasDatabaseName(DatabaseConstraints.FinancialMovementItems.BusinessProduct);
            itemBuilder
                .HasIndex(financialMovementItem => new
                {
                    financialMovementItem.BusinessId,
                    financialMovementItem.ProductCategoryId,
                })
                .HasFilter("product_category_id IS NOT NULL")
                .HasDatabaseName(
                    DatabaseConstraints.FinancialMovementItems.BusinessProductCategory
                );

            itemBuilder
                .HasOne<Category>()
                .WithMany()
                .HasForeignKey(financialMovementItem => new
                {
                    financialMovementItem.CategoryId,
                    financialMovementItem.BusinessId,
                })
                .HasPrincipalKey(category => new { category.Id, category.BusinessId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.CategoryBusiness);
            itemBuilder
                .HasOne<Product>()
                .WithMany()
                .HasForeignKey(financialMovementItem => financialMovementItem.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.Product);
            itemBuilder
                .HasOne<ProductCategory>()
                .WithMany()
                .HasForeignKey(financialMovementItem => new
                {
                    financialMovementItem.ProductCategoryId,
                    financialMovementItem.BusinessId,
                })
                .HasPrincipalKey(productCategory => new
                {
                    productCategory.Id,
                    productCategory.BusinessId,
                })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    DatabaseConstraints.FinancialMovementItems.ProductCategoryBusiness
                );
            itemBuilder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(financialMovementItem => financialMovementItem.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.CreatedByUser);
            itemBuilder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(financialMovementItem => financialMovementItem.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.UpdatedByUser);
        }
    }
}
