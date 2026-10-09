using FluentAssertions;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.Products.Interfaces;
using Mype.Application.Products.Models;
using Mype.Application.Products.Queries.ListProducts;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.Permissions.Constants;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Application.Products.Queries.ListProducts
{
    public class ListProductsQueryHandlerTests
    {
        private const string DisplayName =
            "Bodega";

        private const string CurrencyCode =
            "PEN";

        private const string RoleCode =
            "CUSTOM";

        private const string CategoryName =
            "Productos";

        private const string ProductName =
            "Gaseosa 500 ml";

        private const string Search =
            "  gaseosa  500 ml ";

        private const string NormalizedSearch =
            "GASEOSA 500 ML";

        private static readonly Guid BusinessId =
            Guid.NewGuid();

        private static readonly Guid CurrentUserId =
            Guid.NewGuid();

        private static readonly Guid MembershipId =
            Guid.NewGuid();

        private static readonly Guid RoleId =
            Guid.NewGuid();

        private static readonly Guid CategoryId =
            Guid.NewGuid();

        private static readonly Guid ProductId =
            Guid.NewGuid();

        private readonly Mock<
            IBusinessMembershipRepository
        > _membershipRepositoryMock = new();

        private readonly Mock<
            IPermissionRepository
        > _permissionRepositoryMock = new();

        private readonly Mock<
            ICategoryRepository
        > _categoryRepositoryMock = new();

        private readonly Mock<
            IProductRepository
        > _productRepositoryMock = new();

        private readonly ListProductsQueryHandler
            _handler;

        public ListProductsQueryHandlerTests()
        {
            _handler = new ListProductsQueryHandler(
                _membershipRepositoryMock.Object,
                _permissionRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _productRepositoryMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_Return_Products_And_Normalize_Search()
        {
            SetupValidAccess();

            _productRepositoryMock
                .Setup(repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        NormalizedSearch,
                        CategoryId,
                        true,
                        true,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    CreateProducts()
                );

            var result = await _handler.Handle(
                CreateQuery(
                    search: Search,
                    categoryId: CategoryId,
                    isActive: true,
                    availableForSale: true
                ),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeNull();
            result.Value.Should().ContainSingle();

            var product = result.Value.Should()
                .ContainSingle()
                .Subject;

            product.Id.Should().Be(ProductId);
            product.BusinessId.Should().Be(BusinessId);
            product.CategoryId.Should().Be(CategoryId);
            product.CategoryName.Should().Be(CategoryName);
            product.Name.Should().Be(ProductName);
            product.SalePrice.Should().Be(3.50m);
            product.UnitCost.Should().Be(2.20m);
            product.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_Should_Treat_Whitespace_Search_As_Not_Provided()
        {
            SetupValidAccess();

            _productRepositoryMock
                .Setup(repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        null,
                        null,
                        null,
                        false,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Array.Empty<
                        ProductListItemProjection
                    >()
                );

            var result = await _handler.Handle(
                CreateQuery(search: "   "),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();

            _productRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        BusinessId,
                        null,
                        null,
                        null,
                        false,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_Should_Return_CategoryNotFound_When_Filter_Category_Does_Not_Belong_To_Business()
        {
            SetupValidAccess(
                categoryExists: false
            );

            var result = await _handler.Handle(
                CreateQuery(categoryId: CategoryId),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                ListProductsErrors.CategoryNotFound
            );

            VerifyProductsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_ProductAccessForbidden_When_Permission_Is_Missing()
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                Array.Empty<string>()
            );

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                ListProductsErrors
                    .ProductAccessForbidden
            );

            VerifyCategoryWasNotQueried();
            VerifyProductsWereNotQueried();
        }

        [Fact]
        public async Task Handle_Should_Return_BusinessAccessForbidden_When_Context_Does_Not_Exist()
        {
            SetupContext(null);

            var result = await _handler.Handle(
                CreateQuery(),
                CancellationToken.None
            );

            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.Error.Should().BeSameAs(
                ListProductsErrors
                    .BusinessAccessForbidden
            );

            VerifyPermissionsWereNotQueried();
            VerifyCategoryWasNotQueried();
            VerifyProductsWereNotQueried();
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

            result.Error.Should().BeSameAs(
                ListProductsErrors
                    .BusinessAccessForbidden
            );

            VerifyPermissionsWereNotQueried();
            VerifyProductsWereNotQueried();
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

            result.Error.Should().BeSameAs(
                ListProductsErrors
                    .BusinessUnavailable
            );

            VerifyPermissionsWereNotQueried();
            VerifyProductsWereNotQueried();
        }

        private void SetupValidAccess(
            bool categoryExists = true
        )
        {
            SetupContext(
                CreateContext()
            );

            SetupPermissions(
                new[]
                {
                    SystemPermissions.ProductRead.Code
                }
            );

            _categoryRepositoryMock
                .Setup(repository =>
                    repository
                        .ExistsByIdAndBusinessAsync(
                            CategoryId,
                            BusinessId,
                            It.IsAny<CancellationToken>()
                        )
                )
                .ReturnsAsync(categoryExists);
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
                            It.IsAny<CancellationToken>()
                        )
                )
                .ReturnsAsync(context);
        }

        private void SetupPermissions(
            string[] permissions
        )
        {
            _permissionRepositoryMock
                .Setup(repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            RoleId,
                            It.IsAny<CancellationToken>()
                        )
                )
                .ReturnsAsync(permissions);
        }

        private void VerifyPermissionsWereNotQueried()
        {
            _permissionRepositoryMock.Verify(
                repository =>
                    repository
                        .ListActiveCodesByRoleIdAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<CancellationToken>()
                        ),
                Times.Never
            );
        }

        private void VerifyCategoryWasNotQueried()
        {
            _categoryRepositoryMock.Verify(
                repository =>
                    repository
                        .ExistsByIdAndBusinessAsync(
                            It.IsAny<Guid>(),
                            It.IsAny<Guid>(),
                            It.IsAny<CancellationToken>()
                        ),
                Times.Never
            );
        }

        private void VerifyProductsWereNotQueried()
        {
            _productRepositoryMock.Verify(
                repository =>
                    repository.ListByBusinessAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<string>(),
                        It.IsAny<Guid?>(),
                        It.IsAny<bool?>(),
                        It.IsAny<bool>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        private static BusinessContextProjection
            CreateContext(
                BusinessStatus businessStatus =
                    BusinessStatus.Active,
                BusinessMembershipStatus
                    membershipStatus =
                        BusinessMembershipStatus.Active,
                bool roleIsActive = true
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
                RoleCode,
                roleIsActive
            );
        }

        private static ListProductsQuery CreateQuery(
            string search = null,
            Guid? categoryId = null,
            bool? isActive = null,
            bool availableForSale = false
        )
        {
            return new ListProductsQuery
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
                Search = search,
                CategoryId = categoryId,
                IsActive = isActive,
                AvailableForSale = availableForSale
            };
        }

        private static ProductListItemProjection[]
            CreateProducts()
        {
            return
            [
                new ProductListItemProjection(
                    ProductId,
                    BusinessId,
                    CategoryId,
                    CategoryName,
                    ProductName,
                    3.50m,
                    2.20m,
                    true
                )
            ];
        }
    }
}
