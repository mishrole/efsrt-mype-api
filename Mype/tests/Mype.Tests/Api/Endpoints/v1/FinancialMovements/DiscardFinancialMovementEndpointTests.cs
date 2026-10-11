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
using Mype.Application.FinancialMovements.Commands.DiscardFinancialMovement;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class DiscardFinancialMovementEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Send_Command_And_Return_Ok()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var evidenceId = Guid.NewGuid();
            var now = new DateTimeOffset(2026, 10, 11, 8, 0, 0, TimeSpan.Zero);
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            DiscardFinancialMovementCommand captured = null;
            user.Setup(provider => provider.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(service =>
                    service.Send(
                        It.IsAny<DiscardFinancialMovementCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (
                        IRequest<Result<DiscardFinancialMovementResult>> request,
                        CancellationToken _
                    ) => captured = (DiscardFinancialMovementCommand)request
                )
                .ReturnsAsync(
                    Result<DiscardFinancialMovementResult>.Success(
                        new(
                            movementId,
                            businessId,
                            FinancialMovementType.Sale,
                            FinancialMovementStatus.Discarded,
                            new DateOnly(2026, 10, 11),
                            10m,
                            now,
                            2,
                            []
                        )
                    )
                );
            IReadOnlyCollection<MovementEvidenceVersion> versions = [new(evidenceId, 4)];

            var response = await DiscardFinancialMovementEndpoint.DoAsync(
                businessId,
                movementId,
                new DiscardFinancialMovementEndpoint.Request(3, versions),
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
            captured.EvidenceVersions.Should().BeEquivalentTo(versions);
        }
    }
}
