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
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementDraft;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public class GetFinancialMovementDraftEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Return_Ok_And_Send_Query()
        {
            var b = Guid.NewGuid();
            var m = Guid.NewGuid();
            var u = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(x => x.GetCurrentUserId()).Returns(u);
            sender
                .Setup(x =>
                    x.Send(
                        It.IsAny<GetFinancialMovementDraftQuery>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Result<FinancialMovementDraftDetailResult>.Success(
                        new(
                            m,
                            b,
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
            var result = await GetFinancialMovementDraftEndpoint.DoAsync(
                b,
                m,
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );
            result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(200);
            sender.Verify(
                x =>
                    x.Send(
                        It.Is<GetFinancialMovementDraftQuery>(q =>
                            q.BusinessId == b && q.MovementId == m && q.CurrentUserId == u
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
