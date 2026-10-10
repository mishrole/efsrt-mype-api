using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.FinancialMovements;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Commands.AddSaleItem;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public class AddSaleItemEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Send_Command_And_Return_Expected_Status()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(x => x.Send(It.IsAny<AddSaleItemCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    Result<FinancialMovementItemMaintenanceResult>.Success(
                        new(
                            new(
                                itemId,
                                productId,
                                "Gaseosa",
                                categoryId,
                                "Productos",
                                2m,
                                3m,
                                2m,
                                6m,
                                4m,
                                2m,
                                true,
                                null,
                                1
                            ),
                            6m,
                            8
                        )
                    )
                );
            var request = new AddSaleItemEndpoint.AddSaleItemRequest(productId, 2m, 3m, 7);
            var response = await AddSaleItemEndpoint.DoAsync(
                businessId,
                movementId,
                request,
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );
            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(201);
            sender.Verify(
                x =>
                    x.Send(
                        It.Is<AddSaleItemCommand>(c =>
                            c.BusinessId == businessId
                            && c.MovementId == movementId
                            && c.ProductId == productId
                            && c.MovementVersion == 7
                            && c.CurrentUserId == userId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
