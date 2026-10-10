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
using Mype.Application.Products.Commands.CreateProduct;

namespace Mype.Tests.Api.Endpoints.v1.Products
{
    public class CreateProductEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Return_Created_And_Send_Command()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var senderMock = new Mock<ISender>();
            var userContextProviderMock = new Mock<IUserContextProvider>();

            userContextProviderMock.Setup(provider => provider.GetCurrentUserId()).Returns(userId);

            senderMock
                .Setup(sender =>
                    sender.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<CreateProductResult>.Success(
                        new CreateProductResult
                        {
                            Id = productId,
                            BusinessId = businessId,
                            CategoryId = categoryId,
                            Name = "Gaseosa",
                        }
                    )
                );

            var result = await CreateProductEndpoint.DoAsync(
                businessId,
                new CreateProductEndpoint.CreateProductRequest("Gaseosa", categoryId, 3.50m, 2.20m),
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status201Created);

            senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<CreateProductCommand>(command =>
                            command.BusinessId == businessId
                            && command.CurrentUserId == userId
                            && command.CategoryId == categoryId
                            && command.Name == "Gaseosa"
                            && command.SalePrice == 3.50m
                            && command.UnitCost == 2.20m
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
