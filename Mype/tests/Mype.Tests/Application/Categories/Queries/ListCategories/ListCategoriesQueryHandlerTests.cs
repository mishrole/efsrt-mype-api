using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Categories.Models;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Application.Common;
using Mype.Application.Permissions.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Categories;
using Mype.Domain.Permissions.Constants;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Categories.Queries
    .ListCategories
{
    public class ListCategoriesQueryHandlerTests
    {
        private const string DisplayName = "Bodega Central";

        private const string CurrencyCode = "PEN";

        private const string RoleCode = "OWNER";

        private const string SaleCategoryName = "Productos";

        private const string ExpenseCategoryName = "Servicios";

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid MembershipId = Guid.NewGuid();

        private static readonly Guid RoleId = Guid.NewGuid();

        private static readonly Guid SaleCategoryId = Guid.NewGuid();

        private static readonly Guid ExpenseCategoryId = Guid.NewGuid();

        private readonly Mock<
            IBusinessMembershipRepository
        > _membershipRepositoryMock = new();

        private readonly Mock<
            IPermissionRepository
        > _permissionRepositoryMock = new();

        private readonly Mock<
            ICategoryRepository
        > _categoryRepositoryMock = new();

        private readonly ListCategoriesQueryHandler
            _handler;

        public ListCategoriesQueryHandlerTests()
        {
            _handler =
                new ListCategoriesQueryHandler(
                    _membershipRepositoryMock.Object,
                    _permissionRepositoryMock.Object,
                    _categoryRepositoryMock.Object
                );
        }

        [Fact]
        public async Task Handle_Should_Return_Categories_When_Access_Is_Valid()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                SystemPermissions.CategoryRead.Code
            );

            SetupCategories(
                CreateCategories()
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().HaveCount(2);

            result.Value.Should().Contain(
                category =>
                    category.Id ==
                        SaleCategoryId &&
                    category.BusinessId ==
                        BusinessId &&
                    category.Name ==
                        SaleCategoryName &&
                    category.Type ==
                        CategoryType.Sale &&
                    category.IsDefault &&
                    category.IsActive
            );

            result.Value.Should().Contain(
                category =>
                    category.Id ==
                        ExpenseCategoryId &&
                    category.BusinessId ==
                        BusinessId &&
                    category.Name ==
                        ExpenseCategoryName &&
                    category.Type ==
                        CategoryType.Expense &&
                    category.IsDefault &&
                    category.IsActive
            );

            VerifyContextWasQueried();
            VerifyPermissionsWereQueried();
            VerifyCategoriesWereQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_Collection_When_No_Categories_Match()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                SystemPermissions.CategoryRead.Code
            );

            SetupCategories(
                Array.Empty<
                    CategoryListItemProjection
                >()
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEmpty();

            VerifyCategoriesWereQueried();
        }

        [Fact]
        public async Task Handle_Should_Forward_Type_Filter()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                SystemPermissions.CategoryRead.Code
            );

            var query = CreateQuery();
            query.Type = CategoryType.Sale;

            SetupCategories(
                Array.Empty<
                    CategoryListItemProjection
                >(),
                type: CategoryType.Sale
            );

            await _handler.Handle(
                query,
                CancellationToken.None
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        CategoryType.Sale,
                        null,
                        It.IsAny<
                            CancellationToken
                        >()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Forward_IsActive_Filter()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                SystemPermissions.CategoryRead.Code
            );

            var query = CreateQuery();
            query.IsActive = false;

            SetupCategories(
                Array.Empty<
                    CategoryListItemProjection
                >(),
                isActive: false
            );

            await _handler.Handle(
                query,
                CancellationToken.None
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        null,
                        false,
                        It.IsAny<
                            CancellationToken
                        >()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Forward_Both_Filters()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                SystemPermissions.CategoryRead.Code
            );

            var query = CreateQuery();
            query.Type = CategoryType.Expense;
            query.IsActive = true;

            SetupCategories(
                Array.Empty<
                    CategoryListItemProjection
                >(),
                CategoryType.Expense,
                true
            );

            await _handler.Handle(
                query,
                CancellationToken.None
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        CategoryType.Expense,
                        true,
                        It.IsAny<
                            CancellationToken
                        >()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Membership_Does_Not_Exist()
        {
            SetupContext(null);

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            AssertFailure(
                result,
                ListCategoriesErrors
                    .BusinessAccessForbidden
            );

            VerifyPermissionsWereNotQueried();
            VerifyCategoriesWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Membership_Is_Inactive()
        {
            SetupContext(
                CreateContext(
                    membershipStatus:
                        BusinessMembershipStatus
                            .Inactive
                )
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            AssertFailure(
                result,
                ListCategoriesErrors
                    .BusinessAccessForbidden
            );

            VerifyPermissionsWereNotQueried();
            VerifyCategoriesWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Role_Is_Inactive()
        {
            SetupContext(
                CreateContext(
                    roleIsActive: false
                )
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            AssertFailure(
                result,
                ListCategoriesErrors
                    .BusinessAccessForbidden
            );

            VerifyPermissionsWereNotQueried();
            VerifyCategoriesWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessUnavailable_When_Business_Is_Inactive()
        {
            SetupContext(
                CreateContext(
                    businessStatus:
                        BusinessStatus.Inactive
                )
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            AssertFailure(
                result,
                ListCategoriesErrors
                    .BusinessUnavailable
            );

            VerifyPermissionsWereNotQueried();
            VerifyCategoriesWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Not_Reveal_Business_Status_When_Membership_Is_Inactive()
        {
            SetupContext(
                CreateContext(
                    businessStatus:
                        BusinessStatus.Inactive,
                    membershipStatus:
                        BusinessMembershipStatus
                            .Inactive
                )
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            result.Error.Should().BeSameAs(
                ListCategoriesErrors
                    .BusinessAccessForbidden
            );

            result.Error.Should().NotBeSameAs(
                ListCategoriesErrors
                    .BusinessUnavailable
            );

            VerifyPermissionsWereNotQueried();
            VerifyCategoriesWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_CategoryAccessForbidden_When_CategoryRead_Is_Missing()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                SystemPermissions.BusinessRead.Code
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            AssertFailure(
                result,
                ListCategoriesErrors
                    .CategoryAccessForbidden
            );

            VerifyPermissionsWereQueried();
            VerifyCategoriesWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Allow_CategoryRead_Without_Direct_Role_Code_Check()
        {
            SetupContext(
                CreateContext(
                    roleCode: "CUSTOM_ROLE"
                )
            );

            SetupPermissions(
                SystemPermissions.CategoryRead.Code
            );

            SetupCategories(
                CreateCategories()
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);

            VerifyCategoriesWereQueried();
        }

        [Fact]
        public async Task Handle_Should_Forward_CancellationToken_To_All_Repositories()
        {
            using var cancellationTokenSource =
                new CancellationTokenSource();

            var cancellationToken =
                cancellationTokenSource.Token;

            _membershipRepositoryMock
                .Setup(repository =>
                    repository
                        .GetContextByBusinessAndUserAsync(
                            BusinessId,
                            CurrentUserId,
                            cancellationToken
                        )
                )
                .ReturnsAsync(
                    CreateContext()
                );

            _permissionRepositoryMock
                .Setup(repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            RoleId,
                            cancellationToken
                        )
                )
                .ReturnsAsync(
                    new[]
                    {
                        SystemPermissions
                            .CategoryRead.Code
                    }
                );

            _categoryRepositoryMock
                .Setup(repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        null,
                        null,
                        cancellationToken
                    )
                )
                .ReturnsAsync(
                    Array.Empty<
                        CategoryListItemProjection
                    >()
                );

            await _handler.Handle(
                CreateQuery(),
                cancellationToken
            );

            _membershipRepositoryMock.Verify(
                repository =>
                    repository
                        .GetContextByBusinessAndUserAsync(
                            BusinessId,
                            CurrentUserId,
                            cancellationToken
                        ),
                Times.Once
            );

            _permissionRepositoryMock.Verify(
                repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            RoleId,
                            cancellationToken
                        ),
                Times.Once
            );

            _categoryRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        null,
                        null,
                        cancellationToken
                    ),
                Times.Once
            );
        }

        private void SetupContext(
            BusinessContextProjection context
        )
        {
            _membershipRepositoryMock
                .Setup(repository =>
                    repository
                        .GetContextByBusinessAndUserAsync(
                            BusinessId,
                            CurrentUserId,
                            It.IsAny<
                                CancellationToken
                            >()
                        )
                )
                .ReturnsAsync(context);
        }

        private void SetupPermissions(
            params string[] permissions
        )
        {
            _permissionRepositoryMock
                .Setup(repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<
                                CancellationToken
                            >()
                        )
                )
                .ReturnsAsync(permissions);
        }

        private void SetupCategories(
            IReadOnlyCollection<
                CategoryListItemProjection
            > categories,
            CategoryType? type = null,
            bool? isActive = null
        )
        {
            _categoryRepositoryMock
                .Setup(repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        type,
                        isActive,
                        It.IsAny<
                            CancellationToken
                        >()
                    )
                )
                .ReturnsAsync(categories);
        }

        private void VerifyContextWasQueried()
        {
            _membershipRepositoryMock.Verify(
                repository =>
                    repository
                        .GetContextByBusinessAndUserAsync(
                            BusinessId,
                            CurrentUserId,
                            It.IsAny<
                                CancellationToken
                            >()
                        ),
                Times.Once
            );
        }

        private void VerifyPermissionsWereQueried()
        {
            _permissionRepositoryMock.Verify(
                repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<
                                CancellationToken
                            >()
                        ),
                Times.Once
            );
        }

        private void VerifyPermissionsWereNotQueried()
        {
            _permissionRepositoryMock.Verify(
                repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<
                                CancellationToken
                            >()
                        ),
                Times.Never
            );
        }

        private void VerifyCategoriesWereQueried()
        {
            _categoryRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        null,
                        null,
                        It.IsAny<
                            CancellationToken
                        >()
                    ),
                Times.Once
            );
        }

        private void VerifyCategoriesWereNotQueried()
        {
            _categoryRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CategoryType?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<
                            CancellationToken
                        >()
                    ),
                Times.Never
            );
        }

        private static void AssertFailure(
            Result<
                IReadOnlyCollection<
                    CategoryListItemResult
                >
            > result,
            ApplicationError expectedError
        )
        {
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                expectedError
            );
        }

        private static ListCategoriesQuery
            CreateQuery()
        {
            return new ListCategoriesQuery
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId
            };
        }

        private static IReadOnlyCollection<
            CategoryListItemProjection
        > CreateCategories()
        {
            return
            [
                new CategoryListItemProjection(
                    SaleCategoryId,
                    BusinessId,
                    SaleCategoryName,
                    CategoryType.Sale,
                    true,
                    true
                ),
                new CategoryListItemProjection(
                    ExpenseCategoryId,
                    BusinessId,
                    ExpenseCategoryName,
                    CategoryType.Expense,
                    true,
                    true
                )
            ];
        }

        private static BusinessContextProjection
            CreateContext(
                BusinessStatus businessStatus =
                    BusinessStatus.Active,
                BusinessMembershipStatus
                    membershipStatus =
                        BusinessMembershipStatus
                            .Active,
                bool roleIsActive = true,
                string roleCode = RoleCode
            )
        {
            return new BusinessContextProjection(
                BusinessId,
                DisplayName,
                CurrencyCode,
                businessStatus,
                MembershipId,
                membershipStatus,
                RoleId,
                roleCode,
                roleIsActive
            );
        }
    }
}