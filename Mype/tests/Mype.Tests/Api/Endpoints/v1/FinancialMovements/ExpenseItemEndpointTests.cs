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
using Mype.Application.FinancialMovements.Commands.AddExpenseItem;
using Mype.Application.FinancialMovements.Commands.UpdateExpenseItem;
using Mype.Application.FinancialMovements.Models;

namespace Mype.Tests.Api.Endpoints.v1.FinancialMovements
{
    public sealed class ExpenseItemEndpointTests
    {
        [Fact]
        public async Task Add_DoAsync_Should_Send_Command_And_Return_Created()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(x =>
                    x.Send(It.IsAny<AddExpenseItemCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<ExpenseMovementItemMaintenanceResult>.Success(Result(itemId, categoryId))
                );
            var request = new AddExpenseItemEndpoint.AddExpenseItemRequest(
                categoryId,
                "Bolsas",
                2m,
                15.50m,
                7
            );

            var response = await AddExpenseItemEndpoint.DoAsync(
                businessId,
                movementId,
                request,
                sender.Object,
                user.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            response
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status201Created);
            sender.Verify(
                x =>
                    x.Send(
                        It.Is<AddExpenseItemCommand>(command =>
                            command.BusinessId == businessId
                            && command.MovementId == movementId
                            && command.CurrentUserId == userId
                            && command.CategoryId == categoryId
                            && command.Description == "Bolsas"
                            && command.MovementVersion == 7
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task Update_DoAsync_Should_Send_Command_And_Return_Ok()
        {
            var businessId = Guid.NewGuid();
            var movementId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var sender = new Mock<ISender>();
            var user = new Mock<IUserContextProvider>();
            user.Setup(x => x.GetCurrentUserId()).Returns(userId);
            sender
                .Setup(x =>
                    x.Send(It.IsAny<UpdateExpenseItemCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    Result<ExpenseMovementItemMaintenanceResult>.Success(Result(itemId, categoryId))
                );
            var request = new UpdateExpenseItemEndpoint.UpdateExpenseItemRequest(
                categoryId,
                "Bolsas reforzadas",
                3m,
                15.50m,
                9,
                8
            );

            var response = await UpdateExpenseItemEndpoint.DoAsync(
                businessId,
                movementId,
                itemId,
                request,
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
                x =>
                    x.Send(
                        It.Is<UpdateExpenseItemCommand>(command =>
                            command.BusinessId == businessId
                            && command.MovementId == movementId
                            && command.ItemId == itemId
                            && command.CurrentUserId == userId
                            && command.CategoryId == categoryId
                            && command.ItemVersion == 8
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private static ExpenseMovementItemMaintenanceResult Result(Guid itemId, Guid categoryId) =>
            new(
                new ExpenseMovementItemResult(
                    itemId,
                    "Bolsas",
                    categoryId,
                    "Insumos",
                    2m,
                    15.50m,
                    31m,
                    true,
                    null,
                    8
                ),
                31m,
                9
            );
    }
}
