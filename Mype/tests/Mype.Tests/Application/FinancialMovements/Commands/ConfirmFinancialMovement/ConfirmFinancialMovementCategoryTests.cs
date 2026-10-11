using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
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
    public sealed class ConfirmFinancialMovementCategoryTests
    {
        [Fact]
        public async Task Handle_Should_Confirm_Sale_With_Sale_Category()
        {
            var fixture = new Fixture();
            var movement = fixture.CreateSaleMovement(out var categoryId);
            fixture.UseMovement(movement);
            fixture.UseCategory(fixture.CreateCategory(categoryId, CategoryType.Sale));

            var result = await fixture
                .Handler()
                .Handle(fixture.Command(movement), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
        }

        [Fact]
        public async Task Handle_Should_Reject_Sale_With_Expense_Category()
        {
            var fixture = new Fixture();
            var movement = fixture.CreateSaleMovement(out var categoryId);
            fixture.UseMovement(movement);
            fixture.UseCategory(fixture.CreateCategory(categoryId, CategoryType.Expense));

            var result = await fixture
                .Handler()
                .Handle(fixture.Command(movement), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.InvalidSaleItems);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Expense_With_Sale_Category()
        {
            var fixture = new Fixture();
            var movement = fixture.CreateExpenseMovement(out var categoryId);
            fixture.UseMovement(movement);
            fixture.UseCategory(fixture.CreateCategory(categoryId, CategoryType.Sale));

            var result = await fixture
                .Handler()
                .Handle(fixture.Command(movement), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.InvalidExpenseItems);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Reject_Missing_Or_Other_Business_Category()
        {
            var fixture = new Fixture();
            var movement = fixture.CreateExpenseMovement(out _);
            fixture.UseMovement(movement);

            var result = await fixture
                .Handler()
                .Handle(fixture.Command(movement), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.InvalidMovementItems);
            fixture.UnitOfWork.Verify(
                unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task Handle_Should_Allow_Inactive_Category_Of_Expected_Type()
        {
            var fixture = new Fixture();
            var movement = fixture.CreateExpenseMovement(out var categoryId);
            fixture.UseMovement(movement);
            var category = fixture.CreateCategory(categoryId, CategoryType.Expense);
            category.Deactivate(fixture.UserId, fixture.Now.AddMinutes(-1));
            fixture.UseCategory(category);

            var result = await fixture
                .Handler()
                .Handle(fixture.Command(movement), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            category.IsActive.Should().BeFalse();
            movement.Status.Should().Be(FinancialMovementStatus.Confirmed);
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
            public Mock<ICategoryRepository> Categories { get; } = new();
            public Mock<IUnitOfWork> UnitOfWork { get; } = new();
            public Mock<IClock> Clock { get; } = new();

            public Fixture()
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
                    .ReturnsAsync([SystemPermissions.MovementConfirm.Code]);
                UnitOfWork
                    .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);
                Clock.SetupGet(clock => clock.UtcNow).Returns(Now);
            }

            public FinancialMovement CreateSaleMovement(out Guid categoryId)
            {
                categoryId = Guid.NewGuid();
                var movement = FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Sale,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now.AddMinutes(-2)
                );
                var product = Product.Create(
                    BusinessId,
                    categoryId,
                    "Gaseosa",
                    "GASEOSA",
                    4m,
                    2m,
                    UserId,
                    Now.AddMinutes(-2)
                );
                movement.AddSaleItem(product, 2m, 4m, UserId, Now.AddMinutes(-1));
                return movement;
            }

            public FinancialMovement CreateExpenseMovement(out Guid categoryId)
            {
                categoryId = Guid.NewGuid();
                var movement = FinancialMovement.CreateDraft(
                    BusinessId,
                    FinancialMovementType.Expense,
                    new DateOnly(2026, 10, 10),
                    null,
                    "PEN",
                    UserId,
                    Now.AddMinutes(-2)
                );
                movement.AddExpenseItem(categoryId, "Bolsas", 2m, 10m, UserId, Now.AddMinutes(-1));
                return movement;
            }

            public Category CreateCategory(Guid categoryId, CategoryType type)
            {
                var category = Category.CreateDefault(
                    BusinessId,
                    type,
                    "Categoría",
                    $"CATEGORY {Guid.NewGuid():N}".ToUpperInvariant(),
                    UserId,
                    Now.AddMinutes(-3)
                );
                typeof(Category).GetProperty(nameof(Category.Id)).SetValue(category, categoryId);
                return category;
            }

            public void UseMovement(FinancialMovement movement) =>
                Movements
                    .Setup(repository =>
                        repository.GetAggregateTrackedByIdAndBusinessAsync(
                            MovementId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(movement);

            public void UseCategory(Category category) =>
                Categories
                    .Setup(repository =>
                        repository.GetByIdAndBusinessAsync(
                            category.Id,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                    )
                    .ReturnsAsync(category);

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
