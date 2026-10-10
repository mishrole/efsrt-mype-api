using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Domain.Categories;

namespace Mype.Tests.Application.Categories.Queries.ListCategories
{
    public class ListCategoriesQueryValidatorTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private readonly ListCategoriesQueryValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Succeed_When_Query_Has_No_Filters()
        {
            var result = await _validator.ValidateAsync(CreateQuery());

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(CategoryType.Sale)]
        [InlineData(CategoryType.Expense)]
        public async Task Validate_Should_Succeed_When_Type_Is_Supported(CategoryType type)
        {
            var query = CreateQuery();
            query.Type = type;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Should_Succeed_When_IsActive_Is_Provided(bool isActive)
        {
            var query = CreateQuery();
            query.IsActive = isActive;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Validate_Should_Succeed_When_Both_Filters_Are_Provided()
        {
            var query = CreateQuery();
            query.Type = CategoryType.Sale;
            query.IsActive = true;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Validate_Should_Fail_When_BusinessId_Is_Empty()
        {
            var query = CreateQuery();
            query.BusinessId = Guid.Empty;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeFalse();

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(ListCategoriesQuery.BusinessId));
        }

        [Fact]
        public async Task Validate_Should_Fail_When_CurrentUserId_Is_Empty()
        {
            var query = CreateQuery();
            query.CurrentUserId = Guid.Empty;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeFalse();

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(ListCategoriesQuery.CurrentUserId));
        }

        [Fact]
        public async Task Validate_Should_Fail_When_Type_Is_Not_Supported()
        {
            var query = CreateQuery();
            query.Type = (CategoryType)999;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeFalse();

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(ListCategoriesQuery.Type));
        }

        private static ListCategoriesQuery CreateQuery()
        {
            return new ListCategoriesQuery
            {
                BusinessId = BusinessId,
                CurrentUserId = CurrentUserId,
            };
        }
    }
}
