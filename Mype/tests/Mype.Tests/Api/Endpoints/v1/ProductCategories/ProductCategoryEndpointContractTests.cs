using System;
using FluentAssertions;
using Mype.Api.Endpoints.v1.ProductCategories;

namespace Mype.Tests.Api.ProductCategories
{
    public sealed class ProductCategoryEndpointContractTests
    {
        [Fact]
        public void CreateRequest_Should_Allow_Missing_Color()
        {
            var request = new CreateProductCategoryEndpoint.Request("Bebidas", null);
            request.Name.Should().Be("Bebidas");
            request.Color.Should().BeNull();
        }

        [Fact]
        public void UpdateRequest_Should_Require_Concurrency_Data()
        {
            var request = new UpdateProductCategoryEndpoint.Request("Bebidas", "#3F6FC4", 17);
            request.Version.Should().Be(17);
        }

        [Fact]
        public void StatusRequests_Should_Transport_Version()
        {
            new DeactivateProductCategoryEndpoint.Request(5).Version.Should().Be(5);
            new ReactivateProductCategoryEndpoint.Request(6).Version.Should().Be(6);
        }
    }
}
