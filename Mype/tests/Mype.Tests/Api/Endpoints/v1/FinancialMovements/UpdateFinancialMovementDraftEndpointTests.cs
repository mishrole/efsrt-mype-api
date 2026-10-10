using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.FinancialMovements;
using Mype.Application.Common;
using Mype.Application.FinancialMovements.Commands.UpdateFinancialMovementDraft;
using Mype.Domain.FinancialMovements;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements { public class UpdateFinancialMovementDraftEndpointTests { [Fact] public async Task DoAsync_Should_Return_Ok_And_Send_Command() { var businessId = Guid.NewGuid(); var movementId = Guid.NewGuid(); var userId = Guid.NewGuid(); var sender = new Mock<ISender>(); var user = new Mock<IUserContextProvider>(); user.Setup(x => x.GetCurrentUserId()).Returns(userId); sender.Setup(x => x.Send(It.IsAny<UpdateFinancialMovementDraftCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<UpdateFinancialMovementDraftResult>.Success(new(movementId, businessId, FinancialMovementType.Sale, FinancialMovementStatus.Draft, new DateOnly(2026, 10, 10), null, "PEN", 0m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 2))); var result = await UpdateFinancialMovementDraftEndpoint.DoAsync(businessId, movementId, new(new DateOnly(2026, 10, 10), null, 1), sender.Object, user.Object, new DefaultHttpContext(), CancellationToken.None); result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(200); sender.Verify(x => x.Send(It.Is<UpdateFinancialMovementDraftCommand>(c => c.MovementId == movementId && c.Version == 1), It.IsAny<CancellationToken>()), Times.Once); } } }
