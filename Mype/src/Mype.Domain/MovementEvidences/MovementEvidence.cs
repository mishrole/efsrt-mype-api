using System;
using Mype.Domain.Common;

namespace Mype.Domain.MovementEvidences
{
    public sealed class MovementEvidence : Entity
    {
        private MovementEvidence() { }

        private MovementEvidence(
            Guid id,
            Guid businessId,
            Guid movementId,
            Guid? replacesEvidenceId,
            string storageProvider,
            string storageContainer,
            string storageObjectKey,
            string originalFileName,
            string mimeType,
            long sizeBytes,
            Guid userId,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            BusinessId = businessId;
            MovementId = movementId;
            ReplacesEvidenceId = replacesEvidenceId;
            StorageProvider = storageProvider;
            StorageContainer = storageContainer;
            StorageObjectKey = storageObjectKey;
            OriginalFileName = originalFileName;
            MimeType = mimeType;
            SizeBytes = sizeBytes;
            Status = MovementEvidenceStatus.Active;
            UploadedByUserId = userId;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            UploadedAt = utcNow;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }
        public Guid MovementId { get; private set; }
        public Guid? ReplacesEvidenceId { get; private set; }
        public string StorageProvider { get; private set; } = string.Empty;
        public string StorageContainer { get; private set; } = string.Empty;
        public string StorageObjectKey { get; private set; } = string.Empty;
        public string OriginalFileName { get; private set; } = string.Empty;
        public string MimeType { get; private set; } = string.Empty;
        public long SizeBytes { get; private set; }
        public MovementEvidenceStatus Status { get; private set; }
        public EvidenceRemovalReason? RemovalReason { get; private set; }
        public Guid UploadedByUserId { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public Guid UpdatedByUserId { get; private set; }
        public DateTimeOffset UploadedAt { get; private set; }
        public DateTimeOffset? MarkedForPurgeAt { get; private set; }
        public DateTimeOffset? PurgeAfter { get; private set; }
        public DateTimeOffset? PurgedAt { get; private set; }
        public uint Version { get; private set; }

        public bool IsActive() => Status == MovementEvidenceStatus.Active;

        public static MovementEvidence CreateActive(
            Guid businessId,
            Guid movementId,
            Guid? replacesEvidenceId,
            string storageProvider,
            string storageContainer,
            string storageObjectKey,
            string originalFileName,
            string mimeType,
            long sizeBytes,
            Guid userId,
            DateTimeOffset utcNow
        ) =>
            new(
                Guid.NewGuid(),
                businessId,
                movementId,
                replacesEvidenceId,
                storageProvider,
                storageContainer,
                storageObjectKey,
                originalFileName,
                mimeType,
                sizeBytes,
                userId,
                utcNow
            );

        public void MarkPendingPurge(
            EvidenceRemovalReason reason,
            Guid userId,
            DateTimeOffset utcNow,
            DateTimeOffset purgeAfter
        )
        {
            if (!IsActive())
                throw new MovementEvidenceException(MovementEvidenceError.EvidenceNotActive);
            if (purgeAfter <= utcNow)
                throw new MovementEvidenceException(MovementEvidenceError.InvalidPurgeAfter);

            Status = MovementEvidenceStatus.PendingPurge;
            RemovalReason = reason;
            MarkedForPurgeAt = utcNow;
            PurgeAfter = purgeAfter;
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        public void MarkPurged(Guid userId, DateTimeOffset utcNow)
        {
            if (Status != MovementEvidenceStatus.PendingPurge)
                throw new MovementEvidenceException(MovementEvidenceError.EvidenceNotPendingPurge);

            Status = MovementEvidenceStatus.Purged;
            PurgedAt = utcNow;
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }
    }
}
