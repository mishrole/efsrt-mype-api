using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.FinancialMovements;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Queries.ListFinancialMovementDrafts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements { public class ListFinancialMovementDraftsEndpointTests { [Fact] public async Task DoAsync_Should_Return_Ok_And_Send_Query() { var b = Guid.NewGuid(); var u = Guid.NewGuid(); var sender = new Mock<ISender>(); var user = new Mock<IUserContextProvider>(); user.Setup(x => x.GetCurrentUserId()).Returns(u); sender.Setup(x => x.Send(It.IsAny<ListFinancialMovementDraftsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<IReadOnlyCollection<FinancialMovementDraftListItemResult>>.Success(Array.Empty<FinancialMovementDraftListItemResult>())); var result = await ListFinancialMovementDraftsEndpoint.DoAsync(b, sender.Object, user.Object, new DefaultHttpContext(), CancellationToken.None); result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(200); sender.Verify(x => x.Send(It.Is<ListFinancialMovementDraftsQuery>(q => q.BusinessId == b && q.CurrentUserId == u), It.IsAny<CancellationToken>()), Times.Once); } } }
