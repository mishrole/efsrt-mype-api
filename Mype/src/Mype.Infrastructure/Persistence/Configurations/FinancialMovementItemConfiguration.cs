using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.Products;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public sealed class FinancialMovementItemConfiguration
        : IEntityTypeConfiguration<FinancialMovementItem>
    {
        public void Configure(EntityTypeBuilder<FinancialMovementItem> b)
        {
            b.ToTable(
                "financial_movement_items",
                t =>
                {
                    t.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.QuantityPositive,
                        "quantity > 0"
                    );
                    t.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.UnitAmountNonNegative,
                        "unit_amount >= 0"
                    );
                    t.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.SubtotalNonNegative,
                        "subtotal_amount >= 0"
                    );
                    t.HasCheckConstraint(
                        DatabaseConstraints.FinancialMovementItems.UnitCostNonNegative,
                        "unit_cost_snapshot IS NULL OR unit_cost_snapshot >= 0"
                    );
                }
            );
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Description)
                .HasMaxLength(FinancialMovementItemConstraints.DescriptionMaxLength)
                .IsRequired();
            b.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.UnitAmount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.UnitCostSnapshot).HasPrecision(18, 2);
            b.Property(x => x.SubtotalAmount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
            b.Property(x => x.RetiredAt).HasColumnType("timestamp with time zone");
            b.Property(x => x.Version).IsRowVersion();
            b.HasIndex(x => new { x.MovementId, x.IsActive })
                .HasDatabaseName(DatabaseConstraints.FinancialMovementItems.MovementActive);
            b.HasIndex(x => new { x.BusinessId, x.CategoryId })
                .HasDatabaseName(DatabaseConstraints.FinancialMovementItems.BusinessCategory);
            b.HasIndex(x => new { x.BusinessId, x.ProductId })
                .HasFilter("product_id IS NOT NULL")
                .HasDatabaseName(DatabaseConstraints.FinancialMovementItems.BusinessProduct);
            b.HasOne<Category>()
                .WithMany()
                .HasForeignKey(x => new { x.CategoryId, x.BusinessId })
                .HasPrincipalKey(x => new { x.Id, x.BusinessId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.CategoryBusiness);
            b.HasOne<Product>()
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.Product);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.CreatedByUser);
            b.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.UpdatedByUser);
        }
    }
}
