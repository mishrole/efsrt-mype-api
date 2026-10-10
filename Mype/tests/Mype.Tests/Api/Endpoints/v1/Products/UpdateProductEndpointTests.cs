using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Products;
using Mype.Application.Common;
using Mype.Application.Products.Commands.Common;
using Mype.Application.Products.Commands.UpdateProduct;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Mype.Tests.Api.Endpoints.v1.Products
{
    public class UpdateProductEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Return_Ok_And_Send_Command()
        {
            var businessId = Guid.NewGuid(); var productId = Guid.NewGuid(); var userId = Guid.NewGuid(); var categoryId = Guid.NewGuid();
            var sender = new Mock<ISender>(); var user = new Mock<IUserContextProvider>(); user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            sender.Setup(x => x.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProductMaintenanceResult>.Success(new(productId, businessId, Guid.NewGuid(), "Productos", "Gaseosa", 3.5m, 2.2m, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 8)));
            var result = await UpdateProductEndpoint.DoAsync(businessId, productId, new UpdateProductEndpoint.UpdateProductRequest("Gaseosa", categoryId, 3.5m, 2.2m, 7), sender.Object, user.Object, new DefaultHttpContext(), CancellationToken.None);
            result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status200OK);
            sender.Verify(x => x.Send(It.Is<UpdateProductCommand>(c => c.BusinessId == businessId && c.ProductId == productId && c.CurrentUserId == userId && c.Version == 7), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
