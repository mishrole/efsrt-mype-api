using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Commands.AttachMovementEvidence;
using Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence;
using Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.MovementEvidences.Commands
{
    public sealed class MovementEvidenceCommandHandlerTests
    {
        [Fact]
        public async Task Attach_Should_Upload_Add_Touch_Movement_And_Save_Once()
        {
            var fixture = new Fixture();
            var handler = fixture.AttachHandler();

            var result = await handler.Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(MovementEvidenceStatus.Active);
            fixture.Storage.Verify(
                x => x.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
            fixture.Evidences.Verify(
                x => x.AddAsync(It.IsAny<MovementEvidence>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
            fixture.Movements.Verify(x => x.SetOriginalVersion(fixture.Movement, 7), Times.Once);
            fixture.UnitOfWork.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
            fixture.Storage.Verify(
                x => x.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
            fixture.Movement.UpdatedByUserId.Should().Be(fixture.UserId);
            fixture.Movement.UpdatedAt.Should().Be(fixture.Now);
        }

        [Fact]
        public async Task Attach_Should_Compensate_New_Blob_When_Persistence_Fails()
        {
            var fixture = new Fixture();
            fixture
                .UnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            var handler = fixture.AttachHandler();

            var result = await handler.Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceOperationFailed);
            fixture.Storage.Verify(
                x => x.DeleteIfExistsAsync(fixture.ObjectKey, CancellationToken.None),
                Times.Once
            );
        }

        [Fact]
        public async Task Attach_Should_Compensate_And_Return_ConcurrencyConflict()
        {
            var fixture = new Fixture();
            fixture
                .UnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );
            var handler = fixture.AttachHandler();

            var result = await handler.Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
            fixture.Storage.Verify(
                x => x.DeleteIfExistsAsync(fixture.ObjectKey, CancellationToken.None),
                Times.Once
            );
        }

        [Fact]
        public async Task Replace_Should_Mark_Only_Selected_Evidence_And_Add_Replacement()
        {
            var fixture = new Fixture();
            var previous = fixture.ActiveEvidence();
            fixture
                .Evidences.Setup(x =>
                    x.GetTrackedByIdMovementAndBusinessAsync(
                        previous.Id,
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(previous);
            var handler = fixture.ReplaceHandler();

            var result = await handler.Handle(
                fixture.ReplaceCommand(previous.Id),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            previous.Status.Should().Be(MovementEvidenceStatus.PendingPurge);
            previous.RemovalReason.Should().Be(EvidenceRemovalReason.Replaced);
            result.Value.ReplacesEvidenceId.Should().Be(previous.Id);
            result.Value.Status.Should().Be(MovementEvidenceStatus.Active);
            fixture.Evidences.Verify(x => x.SetOriginalVersion(previous, 6), Times.Once);
            fixture.Evidences.Verify(
                x => x.AddAsync(It.IsAny<MovementEvidence>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
            fixture.UnitOfWork.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Replace_Should_Reject_NonActive_Evidence_Without_Upload()
        {
            var fixture = new Fixture();
            var previous = fixture.ActiveEvidence();
            previous.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                fixture.UserId,
                fixture.Now,
                fixture.Now.AddDays(30)
            );
            fixture
                .Evidences.Setup(x =>
                    x.GetTrackedByIdMovementAndBusinessAsync(
                        previous.Id,
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(previous);
            var handler = fixture.ReplaceHandler();

            var result = await handler.Handle(
                fixture.ReplaceCommand(previous.Id),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceNotActive);
            fixture.Storage.Verify(
                x => x.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Remove_Should_Mark_Selected_Evidence_And_Save_Once_Without_Deleting_Blob()
        {
            var fixture = new Fixture();
            var evidence = fixture.ActiveEvidence();
            fixture
                .Evidences.Setup(x =>
                    x.GetTrackedByIdMovementAndBusinessAsync(
                        evidence.Id,
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(evidence);
            var handler = fixture.RemoveHandler();

            var result = await handler.Handle(
                fixture.RemoveCommand(evidence.Id),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            evidence.Status.Should().Be(MovementEvidenceStatus.PendingPurge);
            evidence.RemovalReason.Should().Be(EvidenceRemovalReason.Removed);
            evidence.MarkedForPurgeAt.Should().Be(fixture.Now);
            evidence.PurgeAfter.Should().Be(fixture.Now.AddDays(30));
            fixture.Movements.Verify(x => x.SetOriginalVersion(fixture.Movement, 7), Times.Once);
            fixture.Evidences.Verify(x => x.SetOriginalVersion(evidence, 6), Times.Once);
            fixture.UnitOfWork.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
            fixture.Storage.Verify(
                x => x.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateTimeOffset Now { get; } = new(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);
            public string ObjectKey { get; } =
                "businesses/business/movements/movement/evidences/evidence/token.pdf";
            public FinancialMovement Movement { get; }
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();
            public Mock<IMovementEvidenceRepository> Evidences { get; } = new();
            public Mock<IEvidenceFileValidator> Validator { get; } = new();
            public Mock<IStorageObjectKeyGenerator> KeyGenerator { get; } = new();
            public Mock<IObjectStorage> Storage { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IClock> Clock { get; } = new();
            public Settings Settings { get; } = new();

            public Fixture()
            {
                Movement = FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Expense,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now.AddMinutes(-1)
                );
                Memberships
                    .Setup(x =>
                        x.GetContextByBusinessAndUserAsync(
                            BusinessId,
                            UserId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(
                        new BusinessContextProjection(
                            BusinessId,
                            "Business",
                            "PEN",
                            BusinessStatus.Active,
                            Guid.NewGuid(),
                            BusinessMembershipStatus.Active,
                            RoleId,
                            "OWNER",
                            true
                        )
                    );
                Permissions
                    .Setup(x =>
                        x.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync(new[] { SystemPermissions.MovementUpdate.Code });
                Movements
                    .Setup(x =>
                        x.GetTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(Movement);
                Validator
                    .Setup(x => x.Validate(It.IsAny<EvidenceFile>()))
                    .Returns(
                        Result<ValidatedEvidenceFile>.Success(
                            new("receipt.pdf", "application/pdf", ".pdf", 5)
                        )
                    );
                KeyGenerator
                    .Setup(x => x.Generate(BusinessId, MovementId, It.IsAny<Guid>(), ".pdf"))
                    .Returns(ObjectKey);
                Storage
                    .Setup(x =>
                        x.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync(
                        new StoredObjectMetadata(ObjectKey, "application/pdf", 5, "etag")
                    );
                UnitOfWork
                    .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(x => x.UtcNow).Returns(Now);
            }

            public AttachMovementEvidenceCommandHandler AttachHandler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    Evidences.Object,
                    Validator.Object,
                    KeyGenerator.Object,
                    Settings,
                    Storage.Object,
                    UnitOfWork.Object,
                    Clock.Object
                );

            public ReplaceMovementEvidenceCommandHandler ReplaceHandler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    Evidences.Object,
                    Validator.Object,
                    KeyGenerator.Object,
                    Settings,
                    Storage.Object,
                    UnitOfWork.Object,
                    Clock.Object
                );

            public RemoveMovementEvidenceCommandHandler RemoveHandler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    Evidences.Object,
                    Settings,
                    UnitOfWork.Object,
                    Clock.Object
                );

            public AttachMovementEvidenceCommand AttachCommand() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    File = File(),
                };

            public ReplaceMovementEvidenceCommand ReplaceCommand(Guid evidenceId) =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    EvidenceId = evidenceId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    EvidenceVersion = 6,
                    File = File(),
                };

            public RemoveMovementEvidenceCommand RemoveCommand(Guid evidenceId) =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    EvidenceId = evidenceId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    EvidenceVersion = 6,
                };

            public MovementEvidence ActiveEvidence() =>
                MovementEvidence.CreateActive(
                    Guid.NewGuid(),
                    BusinessId,
                    MovementId,
                    null,
                    "AzureBlob",
                    "evidences",
                    "businesses/old.pdf",
                    "old.pdf",
                    "application/pdf",
                    5,
                    UserId,
                    Now.AddDays(-1)
                );

            private static EvidenceFile File() =>
                new(
                    "receipt.pdf",
                    "application/pdf",
                    5,
                    new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })
                );
        }

        private sealed class Settings : IEvidenceStorageSettings
        {
            public string Provider => "AzureBlob";
            public string ContainerName => "evidences";
            public long MaxSizeBytes => 10 * 1024 * 1024;
            public int ReadUriMinutes => 5;
            public int PurgeRetentionDays => 30;
        }
    }
}
