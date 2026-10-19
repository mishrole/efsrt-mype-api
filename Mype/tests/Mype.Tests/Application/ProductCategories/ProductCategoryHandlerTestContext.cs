using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Mype.Application.BusinessMemberships.Interfaces;
using Mype.Application.BusinessMemberships.Models;
using Mype.Application.Common.Interfaces;
using Mype.Application.Permissions.Interfaces;
using Mype.Application.ProductCategories.Interfaces;
using Mype.Domain.Businesses;
using Mype.Domain.BusinessMemberships;
using Mype.Domain.ProductCategories;

namespace Mype.Tests.Application.ProductCategories
{
    internal sealed class ProductCategoryHandlerTestContext
    {
        internal static readonly Guid BusinessId = Guid.NewGuid();
        internal static readonly Guid CurrentUserId = Guid.NewGuid();
        internal static readonly Guid RoleId = Guid.NewGuid();
        internal static readonly Guid ProductCategoryId = Guid.NewGuid();
        internal static readonly DateTimeOffset UtcNow = new(2026, 10, 18, 20, 0, 0, TimeSpan.Zero);

        internal Mock<IBusinessMembershipRepository> MembershipRepositoryMock { get; } = new();
        internal Mock<IPermissionRepository> PermissionRepositoryMock { get; } = new();
        internal Mock<IProductCategoryRepository> ProductCategoryRepositoryMock { get; } = new();
        internal Mock<IUnitOfWork> UnitOfWorkMock { get; } = new();
        internal Mock<IClock> ClockMock { get; } = new();

        internal void SetupAccess(
            string permissionCode,
            BusinessStatus businessStatus = BusinessStatus.Active,
            BusinessMembershipStatus membershipStatus = BusinessMembershipStatus.Active,
            bool roleIsActive = true,
            bool includePermission = true
        )
        {
            MembershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    new BusinessContextProjection(
                        BusinessId,
                        "Bodega",
                        "PEN",
                        businessStatus,
                        Guid.NewGuid(),
                        membershipStatus,
                        RoleId,
                        "OWNER",
                        roleIsActive
                    )
                );

            PermissionRepositoryMock
                .Setup(repository =>
                    repository.ListActiveCodesByRoleIdAsync(RoleId, It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(includePermission ? new[] { permissionCode } : Array.Empty<string>());

            ClockMock.SetupGet(clock => clock.UtcNow).Returns(UtcNow);
            UnitOfWorkMock
                .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        internal void SetupMissingBusinessContext()
        {
            MembershipRepositoryMock
                .Setup(repository =>
                    repository.GetContextByBusinessAndUserAsync(
                        BusinessId,
                        CurrentUserId,
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync((BusinessContextProjection)null);
        }

        internal static ProductCategory CreateProductCategory(
            string name = "Bebidas",
            string normalizedName = "BEBIDAS",
            bool isActive = true
        )
        {
            var productCategory = ProductCategory.Create(
                BusinessId,
                name,
                normalizedName,
                "#3F6FC4",
                false,
                CurrentUserId,
                UtcNow
            );
            if (!isActive)
            {
                productCategory.Deactivate(CurrentUserId, UtcNow.AddMinutes(1));
            }
            return productCategory;
        }
    }
}
