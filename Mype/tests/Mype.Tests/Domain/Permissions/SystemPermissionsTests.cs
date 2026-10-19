using System.Linq;
using FluentAssertions;
using Mype.Domain.Permissions.Constants;

namespace Mype.Tests.Domain.Permissions
{
    public sealed class SystemPermissionsTests
    {
        private static readonly string[] ProductCategoryPermissionCodes =
        {
            "PRODUCT_CATEGORY_READ",
            "PRODUCT_CATEGORY_CREATE",
            "PRODUCT_CATEGORY_UPDATE",
            "PRODUCT_CATEGORY_DEACTIVATE",
            "PRODUCT_CATEGORY_REACTIVATE",
        };

        [Fact]
        public void All_Should_Contain_Every_ProductCategory_Permission()
        {
            var permissionCodes = SystemPermissions
                .All.Select(permissionDefinition => permissionDefinition.Code)
                .ToArray();

            permissionCodes.Should().Contain(ProductCategoryPermissionCodes);
        }

        [Fact]
        public void Owner_Should_Contain_Every_ProductCategory_Permission()
        {
            var permissionCodes = SystemPermissions
                .Owner.Select(permissionDefinition => permissionDefinition.Code)
                .ToArray();

            permissionCodes.Should().Contain(ProductCategoryPermissionCodes);
        }

        [Fact]
        public void All_Should_Have_Unique_Identifiers_And_Codes()
        {
            SystemPermissions
                .All.Select(permissionDefinition => permissionDefinition.Id)
                .Should()
                .OnlyHaveUniqueItems();

            SystemPermissions
                .All.Select(permissionDefinition => permissionDefinition.Code)
                .Should()
                .OnlyHaveUniqueItems();
        }
    }
}
