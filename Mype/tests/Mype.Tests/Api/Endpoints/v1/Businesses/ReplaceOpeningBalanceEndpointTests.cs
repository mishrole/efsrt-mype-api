using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Application.Businesses.Commands.ReplaceOpeningBalance;
using Mype.Application.Common;

namespace Mype.Tests.Api.Endpoints.v1.Businesses
{
    public sealed class ReplaceOpeningBalanceEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Map_Request_User_Business_And_Version()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            ReplaceOpeningBalanceCommand captured = null;
            user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(x =>
                    x.Send(It.IsAny<ReplaceOpeningBalanceCommand>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (IRequest<Result<ReplaceOpeningBalanceResult>> request, CancellationToken _) =>
                        captured = (ReplaceOpeningBalanceCommand)request
                )
                .ReturnsAsync(
                    Result<ReplaceOpeningBalanceResult>.Success(
                        new(movementId, -250m, new DateOnly(2026, 10, 1), 8)
                    )
                );

            var response = await ReplaceOpeningBalanceEndpoint.DoAsync(
                businessId,
                new ReplaceOpeningBalanceEndpoint.Request(-250m, "Conciliación inicial", 7),
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
            captured.CurrentUserId.Should().Be(userId);
            captured.Amount.Should().Be(-250m);
            captured.Reason.Should().Be("Conciliación inicial");
            captured.MovementVersion.Should().Be(7);
        }
    }
}
