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
        [Fact]
        public async Task List_Should_Apply_Default_Pagination_And_Send_Filters()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
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
                        new([], 1, 20, 0, false, false)
                    )
                );

            var response = await ListFinancialMovementHistoryEndpoint.DoAsync(
                businessId,
                FinancialMovementType.Sale,
                FinancialMovementStatus.Confirmed,
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 10, 31),
                categoryId,
                0,
                0,
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                default
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(200);
            captured.BusinessId.Should().Be(businessId);
            captured.CurrentUserId.Should().Be(userId);
            captured.Page.Should().Be(1);
            captured.PageSize.Should().Be(20);
            captured.CategoryId.Should().Be(categoryId);
        }

        [Fact]
        public async Task Detail_Should_Send_Business_Movement_And_User()
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
                            0,
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
                default
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(200);
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
