using FluentAssertions;
using Mype.Domain.Categories;
using System;

namespace Mype.Tests.Domain.Categories
{
    public class CategoryTests
    {
        private static readonly Guid BusinessId =
            Guid.NewGuid();

        private static readonly Guid UserId =
            Guid.NewGuid();

        private static readonly DateTimeOffset CreatedAt =
            new(
                2026,
                10,
                9,
                12,
                0,
                0,
                TimeSpan.Zero
            );

        [Fact]
        public void CreateDefault_Should_Initialize_Default_Category()
        {
            var category = CreateCategory();

            category.Id.Should().NotBeEmpty();
            category.BusinessId.Should().Be(BusinessId);
            category.Type.Should().Be(CategoryType.Sale);
            category.Name.Should().Be("Productos");
            category.NormalizedName.Should().Be(
                "PRODUCTOS"
            );
            category.IsDefault.Should().BeTrue();
            category.IsActive.Should().BeTrue();
            category.CreatedByUserId.Should().Be(UserId);
            category.UpdatedByUserId.Should().Be(UserId);
            category.CreatedAt.Should().Be(CreatedAt);
            category.UpdatedAt.Should().Be(CreatedAt);
            category.DeactivatedAt.Should().BeNull();
        }

        [Fact]
        public void Deactivate_Should_Set_Category_As_Inactive()
        {
            var category = CreateCategory();
            var currentUserId = Guid.NewGuid();
            var deactivatedAt = CreatedAt.AddHours(1);

            category.Deactivate(
                currentUserId,
                deactivatedAt
            );

            category.IsActive.Should().BeFalse();
            category.DeactivatedAt.Should().Be(
                deactivatedAt
            );
            category.UpdatedByUserId.Should().Be(
                currentUserId
            );
            category.UpdatedAt.Should().Be(
                deactivatedAt
            );
        }

        private static Category CreateCategory()
        {
            return Category.CreateDefault(
                BusinessId,
                CategoryType.Sale,
                "  Productos  ",
                "PRODUCTOS",
                UserId,
                CreatedAt
            );
        }
    }
}
