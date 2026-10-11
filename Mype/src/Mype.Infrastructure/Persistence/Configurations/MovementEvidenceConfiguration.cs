using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.MovementEvidences.Constraints;
using Mype.Domain.Users;
using Mype.Infrastructure.Persistence.Constraints;

namespace Mype.Infrastructure.Persistence.Configurations
{
    public sealed class MovementEvidenceConfiguration : IEntityTypeConfiguration<MovementEvidence>
    {
        public void Configure(EntityTypeBuilder<MovementEvidence> builder)
        {
            builder.ToTable(
                "movement_evidences",
                table =>
                {
                    table.HasCheckConstraint(
                        DatabaseConstraints.MovementEvidences.SizePositive,
                        "size_bytes > 0"
                    );
                    table.HasCheckConstraint(
                        DatabaseConstraints.MovementEvidences.ActiveState,
                        "status <> 1 OR (removal_reason IS NULL AND marked_for_purge_at IS NULL AND purge_after IS NULL AND purged_at IS NULL)"
                    );
                    table.HasCheckConstraint(
                        DatabaseConstraints.MovementEvidences.PendingPurgeState,
                        "status <> 2 OR (removal_reason IS NOT NULL AND marked_for_purge_at IS NOT NULL AND purge_after IS NOT NULL AND purged_at IS NULL)"
                    );
                    table.HasCheckConstraint(
                        DatabaseConstraints.MovementEvidences.PurgedState,
                        "status <> 3 OR purged_at IS NOT NULL"
                    );
                }
            );

            builder.HasKey(evidence => evidence.Id);
            builder.Property(evidence => evidence.Id).ValueGeneratedNever();
            builder.Property(evidence => evidence.BusinessId).IsRequired();
            builder.Property(evidence => evidence.MovementId).IsRequired();
            builder.Property(evidence => evidence.ReplacesEvidenceId);
            builder
                .Property(evidence => evidence.StorageProvider)
                .HasMaxLength(MovementEvidenceConstraints.StorageProviderMaxLength)
                .IsRequired();
            builder
                .Property(evidence => evidence.StorageContainer)
                .HasMaxLength(MovementEvidenceConstraints.StorageContainerMaxLength)
                .IsRequired();
            builder
                .Property(evidence => evidence.StorageObjectKey)
                .HasMaxLength(MovementEvidenceConstraints.StorageObjectKeyMaxLength)
                .IsRequired();
            builder
                .Property(evidence => evidence.OriginalFileName)
                .HasMaxLength(MovementEvidenceConstraints.OriginalFileNameMaxLength)
                .IsRequired();
            builder
                .Property(evidence => evidence.MimeType)
                .HasMaxLength(MovementEvidenceConstraints.MimeTypeMaxLength)
                .IsRequired();
            builder.Property(evidence => evidence.SizeBytes).IsRequired();
            builder.Property(evidence => evidence.Status).IsRequired();
            builder.Property(evidence => evidence.RemovalReason);
            builder.Property(evidence => evidence.UploadedByUserId).IsRequired();
            builder.Property(evidence => evidence.CreatedByUserId).IsRequired();
            builder.Property(evidence => evidence.UpdatedByUserId).IsRequired();
            builder
                .Property(evidence => evidence.UploadedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            builder
                .Property(evidence => evidence.MarkedForPurgeAt)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(evidence => evidence.PurgeAfter)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(evidence => evidence.PurgedAt)
                .HasColumnType("timestamp with time zone");
            builder
                .Property(evidence => evidence.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            builder
                .Property(evidence => evidence.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            builder.Property(evidence => evidence.Version).IsRowVersion();

            builder
                .HasIndex(evidence => new
                {
                    evidence.StorageProvider,
                    evidence.StorageContainer,
                    evidence.StorageObjectKey,
                })
                .IsUnique()
                .HasDatabaseName(DatabaseConstraints.MovementEvidences.StorageObject);
            builder
                .HasIndex(evidence => new
                {
                    evidence.MovementId,
                    evidence.BusinessId,
                    evidence.Status,
                })
                .HasDatabaseName(DatabaseConstraints.MovementEvidences.MovementBusinessStatus);
            builder
                .HasIndex(evidence => new { evidence.BusinessId, evidence.Status })
                .HasDatabaseName(DatabaseConstraints.MovementEvidences.BusinessStatus);
            builder
                .HasIndex(evidence => evidence.PurgeAfter)
                .HasFilter("status = 2")
                .HasDatabaseName(DatabaseConstraints.MovementEvidences.PendingPurge);
            builder
                .HasIndex(evidence => evidence.UploadedByUserId)
                .HasDatabaseName(DatabaseConstraints.MovementEvidences.UploadedByUser);
            builder
                .HasIndex(evidence => evidence.ReplacesEvidenceId)
                .IsUnique()
                .HasFilter("replaces_evidence_id IS NOT NULL")
                .HasDatabaseName(DatabaseConstraints.MovementEvidences.ReplacesEvidence);

            builder
                .HasOne<FinancialMovement>()
                .WithMany()
                .HasForeignKey(evidence => new { evidence.MovementId, evidence.BusinessId })
                .HasPrincipalKey(movement => new { movement.Id, movement.BusinessId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.MovementEvidences.MovementBusiness);
            builder
                .HasOne<MovementEvidence>()
                .WithMany()
                .HasForeignKey(evidence => evidence.ReplacesEvidenceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.MovementEvidences.Replaces);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(evidence => evidence.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.MovementEvidences.UploadedBy);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(evidence => evidence.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.MovementEvidences.CreatedBy);
            builder
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(evidence => evidence.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(DatabaseConstraints.MovementEvidences.UpdatedBy);
        }
    }
}
