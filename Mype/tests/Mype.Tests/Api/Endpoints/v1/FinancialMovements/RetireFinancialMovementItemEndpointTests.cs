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
using Mype.Application.FinancialMovements.Commands.RetireFinancialMovementItem;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public class RetireFinancialMovementItemEndpointTests
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
                .Setup(x =>
                    x.Send(
                        It.IsAny<RetireFinancialMovementItemCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Result<RetiredFinancialMovementItemResult>.Success(
                        new(itemId, false, DateTimeOffset.UtcNow, 7, 0m, 8)
                    )
                );
            var request =
                new RetireFinancialMovementItemEndpoint.RetireFinancialMovementItemRequest(7, 6);
            var response = await RetireFinancialMovementItemEndpoint.DoAsync(
                businessId,
                movementId,
                itemId,
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
                .Be(200);
            sender.Verify(
                x =>
                    x.Send(
                        It.Is<RetireFinancialMovementItemCommand>(c =>
                            c.ItemId == itemId
                            && c.ItemVersion == 6
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
