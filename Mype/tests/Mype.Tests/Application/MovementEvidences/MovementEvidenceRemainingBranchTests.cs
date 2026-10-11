using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Commands.RemoveMovementEvidence;
using Mype.Application.MovementEvidences.Commands.ReplaceMovementEvidence;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Models;
using Mype.Application.MovementEvidences.Queries.ListMovementEvidences;
using Mype.Application.MovementEvidences.Queries.OpenMovementEvidence;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.MovementEvidences
{
    public sealed class MovementEvidenceRemainingBranchTests
    {
        [Fact]
        public async Task Replace_Should_Reject_NonEditable_Movement_Before_Loading_Evidence()
        {
            var fixture = new Fixture();
            fixture
                .Movements.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(fixture.ConfirmedMovement());

            var result = await fixture
                .ReplaceHandler()
                .Handle(fixture.ReplaceCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementNotEditable);
            fixture.Evidences.Verify(
                repository =>
                    repository.GetTrackedByIdMovementAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Replace_Should_Return_Validation_Error_Without_Uploading()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .Validator.Setup(validator => validator.Validate(It.IsAny<EvidenceFile>()))
                .Returns(
                    Mype.Application.Common.Result<ValidatedEvidenceFile>.Failure(
                        fixture.InvalidFileError
                    )
                );

            var result = await fixture
                .ReplaceHandler()
                .Handle(fixture.ReplaceCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.InvalidEvidenceFile);
            fixture.Storage.Verify(
                storage =>
                    storage.UploadAsync(
                        It.IsAny<Mype.Application.Common.Models.StorageUpload>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Replace_Should_Map_Upload_Failure_Without_Mutating_Previous_Evidence()
        {
            var fixture = new Fixture();
            var previous = fixture.ConfigureActiveEvidence();
            fixture
                .Storage.Setup(storage =>
                    storage.UploadAsync(
                        It.IsAny<Mype.Application.Common.Models.StorageUpload>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new InvalidOperationException());

            var result = await fixture
                .ReplaceHandler()
                .Handle(fixture.ReplaceCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceStorageError);
            previous.Status.Should().Be(MovementEvidenceStatus.Active);
            fixture.UnitOfWork.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Replace_Should_Compensate_And_Return_OperationFailed_When_Save_Fails()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .UnitOfWork.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new InvalidOperationException());

            var result = await fixture
                .ReplaceHandler()
                .Handle(fixture.ReplaceCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceOperationFailed);
            fixture.Storage.Verify(
                storage => storage.DeleteIfExistsAsync(fixture.ObjectKey, CancellationToken.None),
                Times.Once
            );
        }

        [Fact]
        public async Task Remove_Should_Reject_NonEditable_Movement_Before_Loading_Evidence()
        {
            var fixture = new Fixture();
            fixture
                .Movements.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(fixture.ConfirmedMovement());

            var result = await fixture
                .RemoveHandler()
                .Handle(fixture.RemoveCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementNotEditable);
            fixture.Evidences.Verify(
                repository =>
                    repository.GetTrackedByIdMovementAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Remove_Should_Reject_NonActive_Evidence_Without_Saving()
        {
            var fixture = new Fixture();
            var evidence = fixture.ConfigureActiveEvidence();
            evidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                fixture.UserId,
                fixture.Now,
                fixture.Now.AddDays(30)
            );

            var result = await fixture
                .RemoveHandler()
                .Handle(fixture.RemoveCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceNotActive);
            fixture.UnitOfWork.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Remove_Should_Return_ConcurrencyConflict()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .UnitOfWork.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );

            var result = await fixture
                .RemoveHandler()
                .Handle(fixture.RemoveCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.ConcurrencyConflict);
        }

        [Fact]
        public async Task Remove_Should_Return_OperationFailed_For_Unexpected_Save_Error()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .UnitOfWork.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new InvalidOperationException());

            var result = await fixture
                .RemoveHandler()
                .Handle(fixture.RemoveCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceOperationFailed);
        }

        [Fact]
        public async Task List_Should_Reject_Missing_Read_Permission_Before_Loading_Movement()
        {
            var fixture = new Fixture();
            fixture
                .Permissions.Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(
                        fixture.RoleId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync([]);

            var result = await fixture
                .ListHandler()
                .Handle(fixture.ListQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementAccessForbidden);
            fixture.Movements.Verify(
                repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Open_Should_Return_MovementNotFound_Before_Loading_Evidence()
        {
            var fixture = new Fixture();
            fixture
                .Movements.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);

            var result = await fixture
                .OpenHandler()
                .Handle(fixture.OpenQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
            fixture.Evidences.Verify(
                repository =>
                    repository.GetTrackedByIdMovementAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Open_Should_Return_EvidenceNotFound_Without_Calling_Storage()
        {
            var fixture = new Fixture();
            fixture
                .Evidences.Setup(repository =>
                    repository.GetTrackedByIdMovementAndBusinessAsync(
                        fixture.EvidenceId,
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((MovementEvidence)null);

            var result = await fixture
                .OpenHandler()
                .Handle(fixture.OpenQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceNotFound);
            fixture.Storage.Verify(
                storage =>
                    storage.CreateTemporaryReadAccessAsync(
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Open_Should_Rethrow_Cancellation()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .Storage.Setup(storage =>
                    storage.CreateTemporaryReadAccessAsync(
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = () =>
                fixture.OpenHandler().Handle(fixture.OpenQuery(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid EvidenceId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public string ObjectKey { get; } =
                "businesses/business/movements/movement/evidences/evidence/token.pdf";
            public DateTimeOffset Now { get; } = new(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);
            public FinancialMovement Movement { get; }
            public Mype.Application.Common.ApplicationError InvalidFileError { get; } =
                new(
                    ErrorCodes.InvalidEvidenceFile,
                    ErrorMessages.InvalidEvidenceFile,
                    ApplicationErrorType.Validation
                );
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
                Movement = DraftMovement();
                Memberships
                    .Setup(repository =>
                        repository.GetContextByBusinessAndUserAsync(
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
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync([
                        SystemPermissions.MovementRead.Code,
                        SystemPermissions.MovementUpdate.Code,
                    ]);
                Movements
                    .Setup(repository =>
                        repository.GetTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(Movement);
                Validator
                    .Setup(validator => validator.Validate(It.IsAny<EvidenceFile>()))
                    .Returns(
                        Mype.Application.Common.Result<ValidatedEvidenceFile>.Success(
                            new("receipt.pdf", "application/pdf", ".pdf", 5)
                        )
                    );
                KeyGenerator
                    .Setup(generator =>
                        generator.Generate(BusinessId, MovementId, It.IsAny<Guid>(), ".pdf")
                    )
                    .Returns(ObjectKey);
                Storage
                    .Setup(storage =>
                        storage.UploadAsync(
                            It.IsAny<Mype.Application.Common.Models.StorageUpload>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(
                        new Mype.Application.Common.Models.StoredObjectMetadata(
                            ObjectKey,
                            "application/pdf",
                            5,
                            "etag"
                        )
                    );
                UnitOfWork
                    .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(clock => clock.UtcNow).Returns(Now);
            }

            public MovementEvidence ConfigureActiveEvidence()
            {
                var evidence = MovementEvidence.CreateActive(
                    EvidenceId,
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
                Evidences
                    .Setup(repository =>
                        repository.GetTrackedByIdMovementAndBusinessAsync(
                            EvidenceId,
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(evidence);
                return evidence;
            }

            public FinancialMovement ConfirmedMovement()
            {
                var movement = DraftMovement();
                SetStatus(movement, FinancialMovementStatus.Confirmed);
                return movement;
            }

            private FinancialMovement DraftMovement() =>
                FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Sale,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now.AddDays(-1)
                );

            private static void SetStatus(
                FinancialMovement movement,
                FinancialMovementStatus status
            )
            {
                var property = typeof(FinancialMovement).GetProperty(
                    nameof(FinancialMovement.Status)
                );
                property.SetValue(movement, status);
            }

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

            public ListMovementEvidencesQueryHandler ListHandler() =>
                new(Memberships.Object, Permissions.Object, Movements.Object, Evidences.Object);

            public OpenMovementEvidenceQueryHandler OpenHandler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    Evidences.Object,
                    Settings,
                    Storage.Object
                );

            public ReplaceMovementEvidenceCommand ReplaceCommand() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    EvidenceId = EvidenceId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    EvidenceVersion = 6,
                    File = new EvidenceFile(
                        "receipt.pdf",
                        "application/pdf",
                        5,
                        new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D])
                    ),
                };

            public RemoveMovementEvidenceCommand RemoveCommand() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    EvidenceId = EvidenceId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    EvidenceVersion = 6,
                };

            public ListMovementEvidencesQuery ListQuery() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                };

            public OpenMovementEvidenceQuery OpenQuery() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    EvidenceId = EvidenceId,
                    CurrentUserId = UserId,
                };
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
