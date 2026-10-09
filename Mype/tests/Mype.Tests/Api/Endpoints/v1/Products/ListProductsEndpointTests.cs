using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Products;
using Mype.Application.Common;
using Mype.Application.Products.Queries.ListProducts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Tests.Api.Endpoints.v1.Products
{
    public class ListProductsEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Return_Ok_And_Send_Query()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            var senderMock = new Mock<ISender>();
            var userContextProviderMock =
                new Mock<IUserContextProvider>();

            userContextProviderMock
                .Setup(provider =>
                    provider.GetCurrentUserId()
                )
                .Returns(userId);

            senderMock
                .Setup(sender => sender.Send(
                    It.IsAny<ListProductsQuery>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(
                    Result<
                        IReadOnlyCollection<
                            ProductListItemResult
                        >
                    >.Success(
                        Array.Empty<
                            ProductListItemResult
                        >()
                    )
                );

            var result =
                await ListProductsEndpoint.DoAsync(
                    businessId,
                    "gaseosa",
                    categoryId,
                    true,
                    true,
                    senderMock.Object,
                    userContextProviderMock.Object,
                    new DefaultHttpContext(),
                    CancellationToken.None
                );

            result.Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);

            senderMock.Verify(
                sender => sender.Send(
                    It.Is<ListProductsQuery>(query =>
                        query.BusinessId == businessId &&
                        query.CurrentUserId == userId &&
                        query.Search == "gaseosa" &&
                        query.CategoryId == categoryId &&
                        query.IsActive == true &&
                        query.AvailableForSale
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task DoAsync_Should_Default_AvailableForSale_To_False()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var senderMock = new Mock<ISender>();

            var userContextProviderMock =
                new Mock<IUserContextProvider>();

            userContextProviderMock
                .Setup(provider =>
                    provider.GetCurrentUserId()
                )
                .Returns(userId);

            senderMock
                .Setup(sender => sender.Send(
                    It.IsAny<ListProductsQuery>(),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(
                    Result<
                        IReadOnlyCollection<
                            ProductListItemResult
                        >
                    >.Success(
                        Array.Empty<
                            ProductListItemResult
                        >()
                    )
                );

            var result =
                await ListProductsEndpoint.DoAsync(
                    businessId,
                    null,
                    null,
                    null,
                    null,
                    senderMock.Object,
                    userContextProviderMock.Object,
                    new DefaultHttpContext(),
                    CancellationToken.None
                );

            result.Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);

            senderMock.Verify(
                sender => sender.Send(
                    It.Is<ListProductsQuery>(query =>
                        query.BusinessId ==
                            businessId &&
                        query.CurrentUserId ==
                            userId &&
                        !query.AvailableForSale
                    ),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }
    }
}
