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
using Mype.Application.FinancialMovements.Commands.CreateFinancialMovement;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public class CreateFinancialMovementEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Return_Created_And_Send_Command()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(x =>
                    x.Send(
                        It.IsAny<CreateFinancialMovementCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Result<CreateFinancialMovementResult>.Success(
                        new(
                            Guid.NewGuid(),
                            businessId,
                            FinancialMovementType.Sale,
                            FinancialMovementStatus.Draft,
                            new DateOnly(2026, 10, 10),
                            null,
                            "PEN",
                            0m,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            1
                        )
                    )
                );
            var result = await CreateFinancialMovementEndpoint.DoAsync(
                businessId,
                new(FinancialMovementType.Sale, new DateOnly(2026, 10, 10), null),
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );
            result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(201);
            sender.Verify(
                x =>
                    x.Send(
                        It.Is<CreateFinancialMovementCommand>(c =>
                            c.BusinessId == businessId && c.CurrentUserId == userId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
