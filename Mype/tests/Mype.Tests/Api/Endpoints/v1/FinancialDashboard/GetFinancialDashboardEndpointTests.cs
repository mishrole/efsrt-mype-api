using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.FinancialDashboard;
using Mype.Application.Common;
using Mype.Application.FinancialDashboard.Enums;
using Mype.Application.FinancialDashboard.Queries.GetFinancialDashboard;

namespace Mype.Tests.Api.Endpoints.v1.FinancialDashboard
{
    public sealed class GetFinancialDashboardEndpointTests
    {
        [Fact]
        public async Task DoAsync_Should_Map_Route_Query_And_Current_User()
        {
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var from = new DateOnly(2026, 10, 1);
            var to = new DateOnly(2026, 10, 31);
            GetFinancialDashboardQuery captured = null;
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            var payload = new FinancialDashboardResult(
                businessId,
                "PEN",
                from,
                to,
                FinancialDashboardGranularity.Month,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                [],
                [],
                []
            );
            sender
                .Setup(x =>
                    x.Send(It.IsAny<GetFinancialDashboardQuery>(), It.IsAny<CancellationToken>())
                )
                .Callback(
                    (IRequest<Result<FinancialDashboardResult>> request, CancellationToken _) =>
                        captured = (GetFinancialDashboardQuery)request
                )
                .ReturnsAsync(Result<FinancialDashboardResult>.Success(payload));
            var response = await GetFinancialDashboardEndpoint.DoAsync(
                businessId,
                from,
                to,
                FinancialDashboardGranularity.Month,
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
            captured.DateFrom.Should().Be(from);
            captured.DateTo.Should().Be(to);
            captured.Granularity.Should().Be(FinancialDashboardGranularity.Month);
        }
    }
}
