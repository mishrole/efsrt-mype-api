using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mype.Application.Products.Queries.GetProductDetail;

namespace Mype.Tests.Application.Products.Queries.GetProductDetail
{
    public class GetProductDetailQueryValidatorTests
    {
        private readonly GetProductDetailQueryValidator _validator = new();

        [Fact]
        public async Task Validate_Should_Succeed_For_Valid_Query()
        {
            var result = await _validator.ValidateAsync(CreateQuery());

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Validate_Should_Fail_For_Empty_Ids()
        {
            var query = new GetProductDetailQuery();

            var result = await _validator.ValidateAsync(query);

            result.Errors.Should().Contain(error => error.PropertyName == nameof(query.BusinessId));

            result.Errors.Should().Contain(error => error.PropertyName == nameof(query.ProductId));

            result
                .Errors.Should()
                .Contain(error => error.PropertyName == nameof(query.CurrentUserId));
        }

        private static GetProductDetailQuery CreateQuery()
        {
            return new GetProductDetailQuery
            {
                BusinessId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
            };
        }
    }
}
