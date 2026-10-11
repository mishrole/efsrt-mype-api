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
using Mype.Application.FinancialMovements.Commands.CancelFinancialMovement;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class CancelFinancialMovementEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Send_Command_And_Return_Ok()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 10, 11, 14, 0, 0, TimeSpan.Zero);
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            CancelFinancialMovementCommand captured = null;
            user.Setup(provider => provider.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(service =>
                    service.Send(
                        It.IsAny<CancelFinancialMovementCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (
                        IRequest<Result<CancelFinancialMovementResult>> request,
                        CancellationToken _
                    ) => captured = (CancelFinancialMovementCommand)request
                )
                .ReturnsAsync(
                    Result<CancelFinancialMovementResult>.Success(
                        new(
                            movementId,
                            businessId,
                            FinancialMovementType.Expense,
                            FinancialMovementStatus.Cancelled,
                            new DateOnly(2026, 10, 11),
                            "PEN",
                            10m,
                            userId,
                            now.AddHours(-1),
                            userId,
                            now,
                            "Registro duplicado",
                            8
                        )
                    )
                );

            var response = await CancelFinancialMovementEndpoint.DoAsync(
                businessId,
                movementId,
                new CancelFinancialMovementEndpoint.Request("Registro duplicado", 7),
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            captured.BusinessId.Should().Be(businessId);
            captured.MovementId.Should().Be(movementId);
            captured.CurrentUserId.Should().Be(userId);
            captured.Reason.Should().Be("Registro duplicado");
            captured.MovementVersion.Should().Be(7);
        }
    }
}
