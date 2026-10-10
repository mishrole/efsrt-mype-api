using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.Products.Queries.ListProducts;

namespace Mype.Tests.Application.Products.Queries.ListProducts
{
    public class ListProductsQueryValidatorTests
    {
        private readonly ListProductsQueryValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Succeed_Without_Filters()
        {
            var result = await _validator.ValidateAsync(CreateQuery());

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Validate_Should_Succeed_With_All_Filters()
        {
            var query = CreateQuery();

            query.Search = "gaseosa";
            query.CategoryId = Guid.NewGuid();
            query.IsActive = true;
            query.AvailableForSale = true;

            var result = await _validator.ValidateAsync(query);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Validate_Should_Fail_For_Empty_Required_Ids()
        {
            var query = CreateQuery();

            query.BusinessId = Guid.Empty;
            query.CurrentUserId = Guid.Empty;

            var result = await _validator.ValidateAsync(query);

            result.Errors.Should().Contain(error => error.PropertyName == nameof(query.BusinessId));

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(query.CurrentUserId));
        }

        [Fact]
        public async Task Validate_Should_Fail_When_CategoryId_Is_Empty()
        {
            var query = CreateQuery();

            query.CategoryId = Guid.Empty;

            var result = await _validator.ValidateAsync(query);

            result.Errors.Should().Contain(error => error.PropertyName == nameof(query.CategoryId));
        }

        private static ListProductsQuery CreateQuery()
        {
            return new ListProductsQuery
            {
                BusinessId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
            };
        }
    }
}
