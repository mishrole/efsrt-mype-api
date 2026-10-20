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
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDraftItems;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class ListFinancialMovementDraftItemsEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Send_Query_With_Route_And_User_Identifiers()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var senderMock = new Mock<ISender>();
            var userContextProviderMock = new Mock<IUserContextProvider>();
            userContextProviderMock
                .Setup(provider => provider.GetCurrentUserId())
                .Returns(currentUserId);
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<ListFinancialMovementDraftItemsQuery>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Result<FinancialMovementDraftItemsResult>.Success(
                        new FinancialMovementDraftItemsResult(
                            movementId,
                            FinancialMovementType.Sale,
                            2,
                            Array.Empty<FinancialMovementDraftItemResult>()
                        )
                    )
                );

            var result = await ListFinancialMovementDraftItemsEndpoint.DoAsync(
                businessId,
                movementId,
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            result
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<ListFinancialMovementDraftItemsQuery>(query =>
                            query.BusinessId == businessId
                            && query.MovementId == movementId
                            && query.CurrentUserId == currentUserId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
