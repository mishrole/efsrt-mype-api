using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.FinancialMovements;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Commands.ConfirmFinancialMovement;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class ConfirmFinancialMovementEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Send_Command_And_Return_Ok()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 10, 10, 20, 0, 0, TimeSpan.Zero);
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            ConfirmFinancialMovementCommand captured = null;
            user.Setup(provider => provider.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(service =>
                    service.Send(
                        It.IsAny<ConfirmFinancialMovementCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (
                        IRequest<Result<ConfirmFinancialMovementResult>> request,
                        CancellationToken _
                    ) => captured = (ConfirmFinancialMovementCommand)request
                )
                .ReturnsAsync(
                    Result<ConfirmFinancialMovementResult>.Success(
                        new(
                            movementId,
                            businessId,
                            FinancialMovementType.Sale,
                            FinancialMovementStatus.Confirmed,
                            new DateOnly(2026, 10, 10),
                            "PEN",
                            10m,
                            6m,
                            4m,
                            userId,
                            now,
                            2
                        )
                    )
                );
            IReadOnlyCollection<FinancialMovementItemVersion> versions = [new(itemId, 4)];

            var response = await ConfirmFinancialMovementEndpoint.DoAsync(
                businessId,
                movementId,
                new ConfirmFinancialMovementEndpoint.Request(3, versions),
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
            captured.MovementVersion.Should().Be(3);
            captured.ItemVersions.Should().BeEquivalentTo(versions);
        }
    }
}
