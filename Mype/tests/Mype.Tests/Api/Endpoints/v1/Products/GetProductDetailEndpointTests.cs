using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Products;
using Mype.Application.Common;
using Mype.Application.Products.Queries.GetProductDetail;
using Mype.Domain.Categories;

namespace Mype.Tests.Api.Endpoints.v1.Products
{
    public class GetProductDetailEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Return_Ok_And_Send_Query()
        {
            var businessId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var senderMock = new Mock<ISender>();
            var userContextProviderMock = new Mock<IUserContextProvider>();

            userContextProviderMock.Setup(provider => provider.GetCurrentUserId()).Returns(userId);

            senderMock
                .Setup(sender =>
                    sender.Send(It.IsAny<GetProductDetailQuery>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<ProductDetailResult>.Success(
                        new ProductDetailResult(
                            productId,
                            businessId,
                            Guid.NewGuid(),
                            "Productos",
                            CategoryType.Sale,
                            "Gaseosa",
                            3.50m,
                            2.20m,
                            true,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            1
                        )
                    )
                );

            var result = await GetProductDetailEndpoint.DoAsync(
                businessId,
                productId,
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);

            senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<GetProductDetailQuery>(query =>
                            query.BusinessId == businessId
                            && query.ProductId == productId
                            && query.CurrentUserId == userId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
