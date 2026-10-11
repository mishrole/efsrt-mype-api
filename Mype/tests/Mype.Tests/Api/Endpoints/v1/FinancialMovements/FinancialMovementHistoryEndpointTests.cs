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
using Mype.Application.FinancialMovements.Queries.GetFinancialMovementHistoryDetail;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementHistory;
using Mype.Domain.FinancialMovements;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class FinancialMovementHistoryEndpointTests
    {
        [Theory]
        [InlineData(0, 0, 1, 20)]
        [InlineData(3, 40, 3, 40)]
        public async Task List_Should_Map_Default_And_Explicit_Pagination(
            int page,
            int pageSize,
            int expectedPage,
            int expectedPageSize
        )
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            ListFinancialMovementHistoryQuery captured = null;
            user.Setup(provider => provider.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(service =>
                    service.Send(
                        It.IsAny<ListFinancialMovementHistoryQuery>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback(
                    (
                        IRequest<Result<FinancialMovementHistoryPageResult>> request,
                        CancellationToken _
                    ) => captured = (ListFinancialMovementHistoryQuery)request
                )
                .ReturnsAsync(
                    Result<FinancialMovementHistoryPageResult>.Success(
                        new([], expectedPage, expectedPageSize, 0, expectedPage > 1, false)
                    )
                );

            var response = await ListFinancialMovementHistoryEndpoint.DoAsync(
                businessId,
                FinancialMovementType.Sale,
                FinancialMovementStatus.Confirmed,
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 10, 31),
                Guid.NewGuid(),
                page,
                pageSize,
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
            captured.Page.Should().Be(expectedPage);
            captured.PageSize.Should().Be(expectedPageSize);
            captured.BusinessId.Should().Be(businessId);
            captured.CurrentUserId.Should().Be(userId);
        }

        [Fact]
        public async Task Detail_Should_Map_Business_Movement_And_User()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(provider => provider.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(service =>
                    service.Send(
                        It.IsAny<GetFinancialMovementHistoryDetailQuery>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Result<FinancialMovementHistoryDetailResult>.Success(
                        new(
                            movementId,
                            businessId,
                            FinancialMovementType.Expense,
                            FinancialMovementStatus.Draft,
                            new DateOnly(2026, 10, 11),
                            null,
                            "PEN",
                            0m,
                            userId,
                            userId,
                            null,
                            null,
                            null,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow,
                            null,
                            null,
                            null,
                            null,
                            null,
                            true,
                            [],
                            1,
                            [],
                            []
                        )
                    )
                );

            var response = await GetFinancialMovementHistoryDetailEndpoint.DoAsync(
                businessId,
                movementId,
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
            sender.Verify(
                service =>
                    service.Send(
                        It.Is<GetFinancialMovementHistoryDetailQuery>(query =>
                            query.BusinessId == businessId
                            && query.MovementId == movementId
                            && query.CurrentUserId == userId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }
    }
}
