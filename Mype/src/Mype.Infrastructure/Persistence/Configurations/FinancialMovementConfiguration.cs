using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.Businesses;
using Mype.Domain.FinancialMovements;
using Mype.Domain.FinancialMovements.Constraints;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public class FinancialMovementConfiguration : IEntityTypeConfiguration<FinancialMovement>
    {
        public void Configure(EntityTypeBuilder<FinancialMovement> builder)
        {
            builder.ToTable("financial_movements");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.BusinessId).IsRequired();
            builder.Property(m => m.Type).IsRequired();
            builder.Property(m => m.Status).IsRequired();
            builder.Property(m => m.MovementDate).HasColumnType("date").IsRequired();
            builder
                .Property(m => m.Description)
                .HasMaxLength(FinancialMovementConstraints.DescriptionMaxLength);
            builder
                .Property(m => m.CurrencyCode)
                .HasMaxLength(FinancialMovementConstraints.CurrencyCodeLength)
                .IsFixedLength()
                .IsRequired();
            builder
                .Property(m => m.TotalAmount)
                .HasPrecision(
                    FinancialMovementConstraints.AmountPrecision,
                    FinancialMovementConstraints.AmountScale
                )
                .IsRequired();
            builder
                .Property(m => m.CancellationReason)
                .HasMaxLength(FinancialMovementConstraints.CancellationReasonMaxLength);
            builder
                .Property(m => m.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            builder
                .Property(m => m.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            builder.Property(m => m.ConfirmedAt).HasColumnType("timestamp with time zone");
            builder.Property(m => m.CancelledAt).HasColumnType("timestamp with time zone");
            builder.Property(m => m.DiscardedAt).HasColumnType("timestamp with time zone");
            builder.Property(m => m.Version).IsRowVersion();
            builder
                .HasMany(m => m.Items)
                .WithOne()
                .HasForeignKey(i => new { i.MovementId, i.BusinessId })
                .HasPrincipalKey(m => new { m.Id, m.BusinessId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovementItems.MovementBusiness);
            builder.Navigation(m => m.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder
                .HasAlternateKey(m => new { m.Id, m.BusinessId })
                .HasName(DatabaseConstraints.FinancialMovements.IdBusiness);
            builder
                .HasIndex(m => new
                {
                    m.BusinessId,
                    m.Status,
                    m.UpdatedAt,
                })
                .HasDatabaseName(DatabaseConstraints.FinancialMovements.BusinessStatusUpdatedAt);
            builder
                .HasIndex(m => new
                {
                    m.BusinessId,
                    m.Type,
                    m.MovementDate,
                })
                .HasDatabaseName(DatabaseConstraints.FinancialMovements.BusinessTypeDate);
            builder
                .HasOne<Business>()
                .WithMany()
                .HasForeignKey(m => m.BusinessId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovements.Business);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovements.CreatedByUser);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovements.UpdatedByUser);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovements.ConfirmedByUser);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.FinancialMovements.CancelledByUser);
        }
    }
}
