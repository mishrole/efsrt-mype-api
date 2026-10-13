using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Application.Common;

namespace Mype.Tests.Api.Endpoints.v1.Businesses
{
    public sealed class CreateBusinessOpeningBalanceEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Map_OpeningBalance_To_Command()
        {
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            var userId = Guid.NewGuid();
            var openingDate = new DateOnly(2026, 10, 1);
            CreateBusinessCommand captured = null;
            user.Setup(provider => provider.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(service =>
                    service.Send(It.IsAny<CreateBusinessCommand>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (IRequest<Result<CreateBusinessResult>> request, CancellationToken _) =>
                        captured = (CreateBusinessCommand)request
                )
                .ReturnsAsync(
                    Result<CreateBusinessResult>.Success(
                        new CreateBusinessResult
                        {
                            BusinessId = Guid.NewGuid(),
                            OpeningBalanceAmount = -500m,
                            OpeningBalanceDate = openingDate,
                        }
                    )
                );

            await CreateBusinessEndpoint.DoAsync(
                new CreateBusinessEndpoint.CreateBusinessRequest(
                    "Bodega",
                    null,
                    null,
                    "PEN",
                    -500m,
                    openingDate
                ),
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            captured.OpeningBalanceAmount.Should().Be(-500m);
            captured.OpeningBalanceDate.Should().Be(openingDate);
            captured.CurrentUserId.Should().Be(userId);
        }
    }
}
