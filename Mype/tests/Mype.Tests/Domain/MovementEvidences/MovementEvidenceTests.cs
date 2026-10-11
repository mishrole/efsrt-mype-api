using System;
using FluentAssertions;
using Mype.Domain.MovementEvidences;

namespace Mype.Tests.Domain.MovementEvidences
{
    public sealed class MovementEvidenceTests
    {
        [Fact]
        public void CreateActive_Should_Initialize_Active_Evidence()
        {
            var evidenceId = Guid.NewGuid();
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);

            var evidence = MovementEvidence.CreateActive(
                evidenceId,
                businessId,
                movementId,
                null,
                "AzureBlob",
                "evidences",
                "businesses/key.pdf",
                "receipt.pdf",
                "application/pdf",
                128,
                userId,
                now
            );

            evidence.Id.Should().Be(evidenceId);
            evidence.BusinessId.Should().Be(businessId);
            evidence.MovementId.Should().Be(movementId);
            evidence.Status.Should().Be(MovementEvidenceStatus.Active);
            evidence.RemovalReason.Should().BeNull();
            evidence.MarkedForPurgeAt.Should().BeNull();
            evidence.PurgeAfter.Should().BeNull();
            evidence.PurgedAt.Should().BeNull();
            evidence.UploadedByUserId.Should().Be(userId);
            evidence.CreatedByUserId.Should().Be(userId);
            evidence.UpdatedByUserId.Should().Be(userId);
            evidence.UploadedAt.Should().Be(now);
            evidence.CreatedAt.Should().Be(now);
            evidence.UpdatedAt.Should().Be(now);
        }

        [Theory]
        [InlineData(EvidenceRemovalReason.Removed)]
        [InlineData(EvidenceRemovalReason.Replaced)]
        public void MarkPendingPurge_Should_Transition_Active_Evidence(EvidenceRemovalReason reason)
        {
            var evidence = CreateEvidence();
            var actorId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 10, 10, 21, 0, 0, TimeSpan.Zero);
            var purgeAfter = now.AddDays(30);

            evidence.MarkPendingPurge(reason, actorId, now, purgeAfter);

            evidence.Status.Should().Be(MovementEvidenceStatus.PendingPurge);
            evidence.RemovalReason.Should().Be(reason);
            evidence.MarkedForPurgeAt.Should().Be(now);
            evidence.PurgeAfter.Should().Be(purgeAfter);
            evidence.PurgedAt.Should().BeNull();
            evidence.UpdatedByUserId.Should().Be(actorId);
            evidence.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void MarkPendingPurge_Should_Reject_NonActive_Evidence()
        {
            var evidence = CreateEvidence();
            var now = DateTimeOffset.UtcNow;
            evidence.MarkPendingPurge(EvidenceRemovalReason.Removed, Guid.NewGuid(), now, now.AddDays(30));

            var action = () => evidence.MarkPendingPurge(
                EvidenceRemovalReason.Replaced,
                Guid.NewGuid(),
                now.AddMinutes(1),
                now.AddDays(31)
            );

            action.Should().Throw<MovementEvidenceException>()
                .Which.Error.Should().Be(MovementEvidenceError.EvidenceNotActive);
        }

        [Fact]
        public void MarkPendingPurge_Should_Reject_NonFuture_PurgeAfter()
        {
            var evidence = CreateEvidence();
            var now = DateTimeOffset.UtcNow;

            var action = () => evidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                Guid.NewGuid(),
                now,
                now
            );

            action.Should().Throw<MovementEvidenceException>()
                .Which.Error.Should().Be(MovementEvidenceError.InvalidPurgeAfter);
        }

        [Fact]
        public void MarkPurged_Should_Transition_PendingPurge_Evidence()
        {
            var evidence = CreateEvidence();
            var now = DateTimeOffset.UtcNow;
            evidence.MarkPendingPurge(EvidenceRemovalReason.Removed, Guid.NewGuid(), now, now.AddDays(30));
            var purgeTime = now.AddDays(31);
            var actorId = Guid.NewGuid();

            evidence.MarkPurged(actorId, purgeTime);

            evidence.Status.Should().Be(MovementEvidenceStatus.Purged);
            evidence.PurgedAt.Should().Be(purgeTime);
            evidence.UpdatedByUserId.Should().Be(actorId);
            evidence.UpdatedAt.Should().Be(purgeTime);
        }

        [Fact]
        public void MarkPurged_Should_Reject_Active_Evidence()
        {
            var evidence = CreateEvidence();

            var action = () => evidence.MarkPurged(Guid.NewGuid(), DateTimeOffset.UtcNow);

            action.Should().Throw<MovementEvidenceException>()
                .Which.Error.Should().Be(MovementEvidenceError.EvidenceNotPendingPurge);
        }

        private static MovementEvidence CreateEvidence() => MovementEvidence.CreateActive(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "AzureBlob",
            "evidences",
            $"businesses/{Guid.NewGuid():N}.pdf",
            "receipt.pdf",
            "application/pdf",
            128,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow
        );
    }
}
