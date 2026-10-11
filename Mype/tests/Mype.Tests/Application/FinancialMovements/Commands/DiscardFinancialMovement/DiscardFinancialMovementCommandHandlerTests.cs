using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.FinancialMovements;
using Mype.Domain.MovementEvidences;
using Mype.Domain.Permissions.Constants;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.DiscardFinancialMovement
{
    public sealed class DiscardFinancialMovementCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Discard_Movement_Without_Evidences()
        {
            var fixture = new Fixture();

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(FinancialMovementStatus.Discarded);
            result.Value.Evidences.Should().BeEmpty();
            fixture.Movements.Verify(
                repository => repository.SetOriginalVersion(fixture.Movement, 7),
                Times.Once
            );
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(CancellationToken.None),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Mark_All_Active_Evidences_PendingPurge()
        {
            var fixture = new Fixture();
            var first = fixture.Evidence("first.pdf");
            var second = fixture.Evidence("second.pdf");
            fixture.UseEvidences(first, second);
            var command = fixture.Command(new(first.Id, 3), new(second.Id, 4));

            var result = await fixture.Handler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Evidences.Should().HaveCount(2);
            foreach (var evidence in new[] { first, second })
            {
                evidence.Status.Should().Be(MovementEvidenceStatus.PendingPurge);
                evidence.RemovalReason.Should().Be(EvidenceRemovalReason.MovementDiscarded);
                evidence.MarkedForPurgeAt.Should().Be(fixture.Now);
                evidence.PurgeAfter.Should().Be(fixture.Now.AddDays(30));
                evidence.UpdatedByUserId.Should().Be(fixture.UserId);
            }
            fixture.Evidences.Verify(
                repository =>
                    repository.SetOriginalVersions(
                        It.IsAny<IReadOnlyCollection<MovementEvidence>>(),
                        It.Is<IReadOnlyDictionary<Guid, uint>>(versions =>
                            versions[first.Id] == 3 && versions[second.Id] == 4
                        )
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Incomplete_Duplicate_Or_Foreign_EvidenceVersions()
        {
            var fixture = new Fixture();
            var evidence = fixture.Evidence("evidence.pdf");
            fixture.UseEvidences(evidence);
            DiscardFinancialMovementCommand[] commands =
            [
                fixture.Command(),
                fixture.Command(
                    new MovementEvidenceVersion(evidence.Id, 1),
                    new MovementEvidenceVersion(evidence.Id, 2)
                ),
                fixture.Command(new MovementEvidenceVersion(Guid.NewGuid(), 1)),
            ];

            foreach (var command in commands)
            {
                var result = await fixture.Handler().Handle(command, CancellationToken.None);
                result.IsSuccess.Should().BeFalse();
                result.Error.Code.Should().Be(ErrorCodes.ValidationError);
            }
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Theory]
        [InlineData(false, true, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, false, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, true, false, ErrorCodes.BusinessUnavailable)]
        public async Task Handle_Should_Reject_Invalid_Access_Context(
            bool membershipActive,
            bool roleActive,
            bool businessActive,
            string code
        )
        {
            var fixture = new Fixture();
            fixture.ConfigureContext(membershipActive, roleActive, businessActive);

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(code);
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Permission_Or_Movement()
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
            var forbidden = await fixture
                .Handler()
                .Handle(fixture.Command(), CancellationToken.None);
            forbidden.Error.Code.Should().Be(ErrorCodes.MovementAccessForbidden);

            var second = new Fixture();
            second
                .Movements.Setup(repository =>
                    repository.GetTrackedByIdAndBusinessAsync(
                        second.MovementId,
                        second.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);
            var missing = await second.Handler().Handle(second.Command(), CancellationToken.None);
            missing.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
        }

        [Theory]
        [InlineData(FinancialMovementStatus.Confirmed)]
        [InlineData(FinancialMovementStatus.Cancelled)]
        [InlineData(FinancialMovementStatus.Discarded)]
        public async Task Handle_Should_Reject_NonDraft_Movement(FinancialMovementStatus status)
        {
            var fixture = new Fixture();
            typeof(FinancialMovement)
                .GetProperty(
                    nameof(FinancialMovement.Status),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
                .SetValue(fixture.Movement, status);

            var result = await fixture.Handler().Handle(fixture.Command(), CancellationToken.None);

            result.Error.Code.Should().Be(ErrorCodes.MovementNotEditable);
        }

        [Fact]
        public async Task Handle_Should_Map_Concurrency_And_Unexpected_Errors_And_Rethrow_Cancellation()
        {
            var concurrency = new Fixture();
            concurrency
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );
            (await concurrency.Handler().Handle(concurrency.Command(), CancellationToken.None))
                .Error.Code.Should()
                .Be(ErrorCodes.ConcurrencyConflict);

            var failure = new Fixture();
            failure
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());
            (await failure.Handler().Handle(failure.Command(), CancellationToken.None))
                .Error.Code.Should()
                .Be(ErrorCodes.MovementDiscardFailed);

            var cancelled = new Fixture();
            cancelled
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
            var action = () =>
                cancelled.Handler().Handle(cancelled.Command(), CancellationToken.None);
            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private sealed class Fixture
        {
            public Guid BusinessId { get; } = Guid.NewGuid();
            public Guid MovementId { get; } = Guid.NewGuid();
            public Guid UserId { get; } = Guid.NewGuid();
            public Guid RoleId { get; } = Guid.NewGuid();
            public DateTimeOffset Now { get; } = new(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);
            public FinancialMovement Movement { get; }
            public Mock<IBusinessMembershipRepository> Memberships { get; } = new();
            public Mock<IPermissionRepository> Permissions { get; } = new();
            public Mock<IFinancialMovementRepository> Movements { get; } = new();
            public Mock<IMovementEvidenceRepository> Evidences { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IClock> Clock { get; } = new();
            public Settings Settings { get; } = new();

            public Fixture()
            {
                Movement = FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Sale,
                    new DateOnly(2026, 10, 11),
                    null,
                    "PEN",
                    UserId,
                    Now.AddMinutes(-1)
                );
                ConfigureContext(true, true, true);
                Permissions
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync([SystemPermissions.MovementDiscard.Code]);
                Movements
                    .Setup(repository =>
                        repository.GetTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(Movement);
                Evidences
                    .Setup(repository =>
                        repository.ListActiveTrackedByMovementAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(Array.Empty<MovementEvidence>());
                UnitOfWork
                    .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(clock => clock.UtcNow).Returns(Now);
            }

            public void ConfigureContext(
                bool membershipActive,
                bool roleActive,
                bool businessActive
            ) =>
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
                            businessActive ? BusinessStatus.Active : BusinessStatus.Inactive,
                            Guid.NewGuid(),
                            membershipActive
                                ? BusinessMembershipStatus.Active
                                : BusinessMembershipStatus.Inactive,
                            RoleId,
                            "OWNER",
                            roleActive
                        )
                    );

            public MovementEvidence Evidence(string name)
            {
                var id = Guid.NewGuid();
                return MovementEvidence.CreateActive(
                    id,
                    BusinessId,
                    MovementId,
                    null,
                    "AzureBlob",
                    "evidences",
                    $"objects/{id:N}.pdf",
                    name,
                    "application/pdf",
                    10,
                    UserId,
                    Now.AddMinutes(-1)
                );
            }

            public void UseEvidences(params MovementEvidence[] evidences) =>
                Evidences
                    .Setup(repository =>
                        repository.ListActiveTrackedByMovementAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(evidences);

            public DiscardFinancialMovementCommand Command(
                params MovementEvidenceVersion[] versions
            ) =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    EvidenceVersions = versions,
                };

            public DiscardFinancialMovementCommandHandler Handler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    Evidences.Object,
                    Settings,
                    UnitOfWork.Object,
                    Clock.Object
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
