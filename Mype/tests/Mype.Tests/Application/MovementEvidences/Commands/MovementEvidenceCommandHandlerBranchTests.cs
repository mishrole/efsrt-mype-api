using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common;
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
    public sealed class MovementEvidenceCommandHandlerBranchTests
    {
        [Fact]
        public async Task Attach_Should_Reject_Missing_Membership_Before_Loading_Movement()
        {
            var fixture = new Fixture();
            fixture
                .Memberships.Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        fixture.BusinessId,
                        fixture.UserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((BusinessContextProjection)null);

            var result = await fixture
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.BusinessAccessForbidden);
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
        public async Task Attach_Should_Reject_Inactive_Business()
        {
            var fixture = new Fixture();
            fixture.ConfigureMembership(
                BusinessStatus.Inactive,
                BusinessMembershipStatus.Active,
                true
            );

            var result = await fixture
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.BusinessUnavailable);
        }

        [Fact]
        public async Task Attach_Should_Reject_Missing_Update_Permission()
        {
            var fixture = new Fixture();
            fixture
                .Permissions.Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(
                        fixture.RoleId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Array.Empty<string>());

            var result = await fixture
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementAccessForbidden);
        }

        [Fact]
        public async Task Attach_Should_Return_MovementNotFound_Without_Validating_File()
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
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
            fixture.Validator.Verify(
                validator => validator.Validate(It.IsAny<EvidenceFile>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Attach_Should_Return_File_Validation_Error_Without_Uploading()
        {
            var fixture = new Fixture();
            fixture
                .Validator.Setup(validator => validator.Validate(It.IsAny<EvidenceFile>()))
                .Returns(Result<ValidatedEvidenceFile>.Failure(fixture.InvalidFileError));

            var result = await fixture
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.InvalidEvidenceFile);
            fixture.Storage.Verify(
                storage =>
                    storage.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Attach_Should_Map_Upload_Failure_Without_Compensation()
        {
            var fixture = new Fixture();
            fixture
                .Storage.Setup(storage =>
                    storage.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new IOException());

            var result = await fixture
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceStorageError);
            fixture.Storage.Verify(
                storage =>
                    storage.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Attach_Should_Compensate_And_Rethrow_When_Upload_Is_Cancelled()
        {
            var fixture = new Fixture();
            fixture
                .Storage.Setup(storage =>
                    storage.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = () =>
                fixture.AttachHandler().Handle(fixture.AttachCommand(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
            fixture.Storage.Verify(
                storage => storage.DeleteIfExistsAsync(fixture.ObjectKey, CancellationToken.None),
                Times.Once
            );
        }

        [Fact]
        public async Task Attach_Should_Ignore_Compensation_Failure_And_Return_OperationFailed()
        {
            var fixture = new Fixture();
            fixture
                .UnitOfWork.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new InvalidOperationException());
            fixture
                .Storage.Setup(storage =>
                    storage.DeleteIfExistsAsync(fixture.ObjectKey, CancellationToken.None)
                )
                .ThrowsAsync(new IOException());

            var result = await fixture
                .AttachHandler()
                .Handle(fixture.AttachCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceOperationFailed);
        }

        [Fact]
        public async Task Replace_Should_Return_EvidenceNotFound_Without_Uploading()
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
                .ReplaceHandler()
                .Handle(fixture.ReplaceCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceNotFound);
            fixture.Storage.Verify(
                storage =>
                    storage.UploadAsync(It.IsAny<StorageUpload>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Replace_Should_Compensate_And_Rethrow_When_Save_Is_Cancelled()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .UnitOfWork.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = () =>
                fixture.ReplaceHandler().Handle(fixture.ReplaceCommand(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
            fixture.Storage.Verify(
                storage => storage.DeleteIfExistsAsync(fixture.ObjectKey, CancellationToken.None),
                Times.Once
            );
        }

        [Fact]
        public async Task Remove_Should_Return_EvidenceNotFound_Without_Saving()
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
                .RemoveHandler()
                .Handle(fixture.RemoveCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceNotFound);
            fixture.UnitOfWork.Verify(
                unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Remove_Should_Rethrow_Cancellation_Without_Storage_Operations()
        {
            var fixture = new Fixture();
            fixture.ConfigureActiveEvidence();
            fixture
                .UnitOfWork.Setup(unitOfWork =>
                    unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())
                )
                .ThrowsAsync(new OperationCanceledException());

            var action = () =>
                fixture.RemoveHandler().Handle(fixture.RemoveCommand(), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
            fixture.Storage.VerifyNoOtherCalls();
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
            public ApplicationError InvalidFileError { get; } =
                new(
                    ErrorCodes.InvalidEvidenceFile,
                    ErrorMessages.InvalidEvidenceFile,
                    Mype.Application.Common.Exceptions.ApplicationErrorType.Validation
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
                Movement = FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Expense,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now.AddMinutes(-1)
                );
                ConfigureMembership(BusinessStatus.Active, BusinessMembershipStatus.Active, true);
                Permissions
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync([SystemPermissions.MovementUpdate.Code]);
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
                        Result<ValidatedEvidenceFile>.Success(
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
                            It.IsAny<StorageUpload>(),
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(
                        new StoredObjectMetadata(ObjectKey, "application/pdf", 5, "etag")
                    );
                UnitOfWork
                    .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(clock => clock.UtcNow).Returns(Now);
            }

            public void ConfigureMembership(
                BusinessStatus businessStatus,
                BusinessMembershipStatus membershipStatus,
                bool roleIsActive
            )
            {
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
                            businessStatus,
                            Guid.NewGuid(),
                            membershipStatus,
                            RoleId,
                            "OWNER",
                            roleIsActive
                        )
                    );
            }

            public void ConfigureActiveEvidence()
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

            public ReplaceMovementEvidenceCommand ReplaceCommand() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    EvidenceId = EvidenceId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    EvidenceVersion = 6,
                    File = File(),
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

            private static EvidenceFile File() =>
                new(
                    "receipt.pdf",
                    "application/pdf",
                    5,
                    new MemoryStream([0x25, 0x50, 0x44, 0x46, 0x2D])
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
