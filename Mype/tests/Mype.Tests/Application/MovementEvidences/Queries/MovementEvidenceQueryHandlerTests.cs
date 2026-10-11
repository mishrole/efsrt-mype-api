using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Interfaces;
using Mype.Application.Common.Models;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.MovementEvidences.Queries.ListMovementEvidences;
using Mype.Application.MovementEvidences.Queries.OpenMovementEvidence;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.MovementEvidences.Queries
{
    public sealed class MovementEvidenceQueryHandlerTests
    {
        [Fact]
        public async Task List_Should_Return_Only_Repository_Results_Without_Storage_Key()
        {
            var fixture = new Fixture();
            var evidence = fixture.ActiveEvidence();
            fixture
                .Evidences.Setup(x =>
                    x.ListActiveByMovementAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(new[] { evidence });
            var handler = new ListMovementEvidencesQueryHandler(
                fixture.Memberships.Object,
                fixture.Permissions.Object,
                fixture.Movements.Object,
                fixture.Evidences.Object
            );

            var result = await handler.Handle(
                new ListMovementEvidencesQuery
                {
                    BusinessId = fixture.BusinessId,
                    MovementId = fixture.MovementId,
                    CurrentUserId = fixture.UserId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            var item = result.Value.Single();
            item.Id.Should().Be(evidence.Id);
            item.OriginalFileName.Should().Be("receipt.pdf");
            item.GetType().GetProperty("StorageObjectKey").Should().BeNull();
        }

        [Fact]
        public async Task List_Should_Return_MovementNotFound_When_Movement_Is_Other_Business()
        {
            var fixture = new Fixture();
            fixture
                .Movements.Setup(x =>
                    x.GetTrackedByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);
            var handler = new ListMovementEvidencesQueryHandler(
                fixture.Memberships.Object,
                fixture.Permissions.Object,
                fixture.Movements.Object,
                fixture.Evidences.Object
            );

            var result = await handler.Handle(
                new ListMovementEvidencesQuery
                {
                    BusinessId = fixture.BusinessId,
                    MovementId = fixture.MovementId,
                    CurrentUserId = fixture.UserId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
            fixture.Evidences.Verify(
                x =>
                    x.ListActiveByMovementAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Open_Should_Create_Five_Minute_Read_Access_For_Active_Evidence()
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
            fixture
                .Storage.Setup(x =>
                    x.CreateTemporaryReadAccessAsync(
                        evidence.StorageObjectKey,
                        TimeSpan.FromMinutes(5),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new TemporaryObjectAccessResult(
                        new Uri("https://storage.test/read"),
                        fixture.Now.AddMinutes(5)
                    )
                );
            var handler = new OpenMovementEvidenceQueryHandler(
                fixture.Memberships.Object,
                fixture.Permissions.Object,
                fixture.Movements.Object,
                fixture.Evidences.Object,
                fixture.Settings,
                fixture.Storage.Object
            );

            var result = await handler.Handle(
                new OpenMovementEvidenceQuery
                {
                    BusinessId = fixture.BusinessId,
                    MovementId = fixture.MovementId,
                    EvidenceId = evidence.Id,
                    CurrentUserId = fixture.UserId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.Url.Should().Be("https://storage.test/read");
            result.Value.ExpiresAt.Should().Be(fixture.Now.AddMinutes(5));
        }

        [Fact]
        public async Task Open_Should_Reject_NonActive_Evidence_Without_Storage_Call()
        {
            var fixture = new Fixture();
            var evidence = fixture.ActiveEvidence();
            evidence.MarkPendingPurge(
                EvidenceRemovalReason.Removed,
                fixture.UserId,
                fixture.Now,
                fixture.Now.AddDays(30)
            );
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
            var handler = new OpenMovementEvidenceQueryHandler(
                fixture.Memberships.Object,
                fixture.Permissions.Object,
                fixture.Movements.Object,
                fixture.Evidences.Object,
                fixture.Settings,
                fixture.Storage.Object
            );

            var result = await handler.Handle(
                new OpenMovementEvidenceQuery
                {
                    BusinessId = fixture.BusinessId,
                    MovementId = fixture.MovementId,
                    EvidenceId = evidence.Id,
                    CurrentUserId = fixture.UserId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceNotActive);
            fixture.Storage.Verify(
                x =>
                    x.CreateTemporaryReadAccessAsync(
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Open_Should_Map_Storage_Failure()
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
            fixture
                .Storage.Setup(x =>
                    x.CreateTemporaryReadAccessAsync(
                        It.IsAny<string>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ThrowsAsync(new InvalidOperationException());
            var handler = new OpenMovementEvidenceQueryHandler(
                fixture.Memberships.Object,
                fixture.Permissions.Object,
                fixture.Movements.Object,
                fixture.Evidences.Object,
                fixture.Settings,
                fixture.Storage.Object
            );

            var result = await handler.Handle(
                new OpenMovementEvidenceQuery
                {
                    BusinessId = fixture.BusinessId,
                    MovementId = fixture.MovementId,
                    EvidenceId = evidence.Id,
                    CurrentUserId = fixture.UserId,
                },
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.EvidenceStorageError);
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateTimeOffset Now { get; } = new(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();
            public Mock<IMovementEvidenceRepository> Evidences { get; } = new();
            public Mock<IObjectStorage> Storage { get; } = new();
            public Settings Settings { get; } = new();

            public Fixture()
            {
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
                    .ReturnsAsync(new[] { SystemPermissions.MovementRead.Code });
                Movements
                    .Setup(x =>
                        x.GetTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(Movement());
            }

            public MovementEvidence ActiveEvidence() =>
                MovementEvidence.CreateActive(
                    Guid.NewGuid(),
                    BusinessId,
                    MovementId,
                    null,
                    "AzureBlob",
                    "evidences",
                    $"businesses/{BusinessId:N}/movements/{MovementId:N}/file.pdf",
                    "receipt.pdf",
                    "application/pdf",
                    100,
                    UserId,
                    Now
                );

            private FinancialMovement Movement() =>
                FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Expense,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now
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
