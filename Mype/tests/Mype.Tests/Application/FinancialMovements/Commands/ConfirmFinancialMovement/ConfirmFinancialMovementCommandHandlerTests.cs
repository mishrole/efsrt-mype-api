using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Common.Exceptions;
using Mype.Application.Common.Interfaces;
using Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement;
using Mype.Application.FinancialMovements.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.FinancialMovements;
using Mype.Domain.Permissions.Constants;
using Mype.Domain.Products;
using Mype.Shared.Constants;

namespace Mype.Tests.Application.FinancialMovements.Commands.ConfirmFinancialMovement
{
    public sealed class ConfirmFinancialMovementCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Confirm_Movement_Set_Versions_And_Save_Once()
        {
            var fixture = new Fixture();
            var movement = fixture.SaleMovement();
            fixture.UseMovement(movement);
            var command = fixture.Command(movement);
            IReadOnlyDictionary<Guid, uint> capturedVersions = null;
            fixture
                .Movements.Setup(repository =>
                    repository.SetOriginalVersions(
                        movement,
                        7,
                        It.IsAny<IReadOnlyDictionary<Guid, uint>>()
                    )
                )
                .Callback(
                    (FinancialMovement _, uint _, IReadOnlyDictionary<Guid, uint> versions) =>
                        capturedVersions = versions
                );

            var result = await fixture.Handler().Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(FinancialMovementStatus.Confirmed);
            result.Value.TotalAmount.Should().Be(12m);
            result.Value.EstimatedCost.Should().Be(6m);
            result.Value.EstimatedMargin.Should().Be(6m);
            capturedVersions.Should().ContainSingle();
            capturedVersions[movement.Items.Single().Id].Should().Be(11);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(CancellationToken.None),
                Times.Once
            );
        }

        [Theory]
        [InlineData(false, true, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, false, true, ErrorCodes.BusinessAccessForbidden)]
        [InlineData(true, true, false, ErrorCodes.BusinessUnavailable)]
        public async Task Handle_Should_Reject_Invalid_Business_Context(
            bool membershipActive,
            bool roleActive,
            bool businessActive,
            string expectedCode
        )
        {
            var fixture = new Fixture();
            fixture.ConfigureContext(membershipActive, roleActive, businessActive);

            var result = await fixture
                .Handler()
                .Handle(fixture.EmptyCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(expectedCode);
            fixture.Movements.Verify(
                repository =>
                    repository.GetAggregateTrackedByIdAndBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Confirm_Permission()
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
                .Handler()
                .Handle(fixture.EmptyCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementAccessForbidden);
        }

        [Fact]
        public async Task Handle_Should_Return_NotFound_For_Other_Business_Without_Revealing_Owner()
        {
            var fixture = new Fixture();
            fixture
                .Movements.Setup(repository =>
                    repository.GetAggregateTrackedByIdAndBusinessAsync(
                        fixture.MovementId,
                        fixture.BusinessId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((FinancialMovement)null);

            var result = await fixture
                .Handler()
                .Handle(fixture.EmptyCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.MovementNotFound);
        }

        [Fact]
        public async Task Handle_Should_Reject_Incomplete_Duplicate_Or_Foreign_ItemVersions()
        {
            var fixture = new Fixture();
            var movement = fixture.SaleMovement();
            fixture.UseMovement(movement);
            var itemId = movement.Items.Single().Id;
            var commands = new[]
            {
                fixture.Command(movement),
                fixture.Command(movement),
                fixture.Command(movement),
            };

            commands[0].ItemVersions = [];
            commands[1].ItemVersions = [new(itemId, 1), new(itemId, 2)];
            commands[2].ItemVersions = [new(Guid.NewGuid(), 1)];

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

        [Fact]
        public async Task Handle_Should_Map_Concurrency_And_Unexpected_Errors()
        {
            var fixture = new Fixture();
            var movement = fixture.SaleMovement();
            fixture.UseMovement(movement);
            fixture
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ApplicationErrorException(
                        ErrorCodes.ConcurrencyConflict,
                        ErrorMessages.ConcurrencyConflict,
                        ApplicationErrorType.Conflict
                    )
                );

            var conflict = await fixture
                .Handler()
                .Handle(fixture.Command(movement), CancellationToken.None);
            conflict.Error.Code.Should().Be(ErrorCodes.ConcurrencyConflict);

            var secondFixture = new Fixture();
            var secondMovement = secondFixture.SaleMovement();
            secondFixture.UseMovement(secondMovement);
            secondFixture
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            var failure = await secondFixture
                .Handler()
                .Handle(secondFixture.Command(secondMovement), CancellationToken.None);
            failure.Error.Code.Should().Be(ErrorCodes.MovementConfirmationFailed);
        }

        [Fact]
        public async Task Handle_Should_Rethrow_Cancellation()
        {
            var fixture = new Fixture();
            var movement = fixture.SaleMovement();
            fixture.UseMovement(movement);
            fixture
                .UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var action = () =>
                fixture.Handler().Handle(fixture.Command(movement), CancellationToken.None);

            await action.Should().ThrowAsync<OperationCanceledException>();
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
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IClock> Clock { get; } = new();
            public Mock<ICategoryRepository> Categories { get; } = new();

            public Fixture()
            {
                ConfigureContext(true, true, true);
                Permissions
                    .Setup(repository =>
                        repository.ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync([SystemPermissions.MovementConfirm.Code]);
                UnitOfWork
                    .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(clock => clock.UtcNow).Returns(Now);
            }

            public void ConfigureContext(
                bool membershipActive,
                bool roleActive,
                bool businessActive
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
            }

            public FinancialMovement SaleMovement()
            {
                var movement = FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Sale,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now.AddMinutes(-1)
                );
                var product = Product.Create(
                    BusinessId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Gaseosa",
                    "GASEOSA",
                    4m,
                    2m,
                    UserId,
                    Now.AddMinutes(-1)
                );
                movement.AddSaleItem(product, 3m, 4m, UserId, Now.AddMinutes(-1));
                return movement;
            }

            public void UseMovement(FinancialMovement movement)
            {
                Movements
                    .Setup(repository =>
                        repository.GetAggregateTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(movement);

                var categoryType =
                    movement.Type == FinancialMovementType.Sale
                        ? CategoryType.Sale
                        : CategoryType.Expense;

                var category = Category.CreateDefault(
                    BusinessId,
                    categoryType,
                    "Categoría",
                    $"CATEGORY {Guid.NewGuid():N}".ToUpperInvariant(),
                    UserId,
                    Now.AddMinutes(-2)
                );

                Categories
                    .Setup(repository =>
                        repository.GetByIdAndBusinessAsync(
                            It.IsAny<Guid>(),
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(category);
            }

            public ConfirmFinancialMovementCommand EmptyCommand() =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    ItemVersions = Array.Empty<FinancialMovementItemVersion>(),
                };

            public ConfirmFinancialMovementCommand Command(FinancialMovement movement) =>
                new()
                {
                    BusinessId = BusinessId,
                    MovementId = MovementId,
                    CurrentUserId = UserId,
                    MovementVersion = 7,
                    ItemVersions = movement
                        .Items.Where(item => item.IsActive)
                        .Select(item => new FinancialMovementItemVersion(item.Id, 11))
                        .ToArray(),
                };

            public ConfirmFinancialMovementCommandHandler Handler() =>
                new(
                    Memberships.Object,
                    Permissions.Object,
                    Movements.Object,
                    Categories.Object,
                    UnitOfWork.Object,
                    Clock.Object
                );
        }
    }
}
