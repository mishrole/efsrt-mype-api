using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.ProductCategories;
using Mype.Application.Common;
using Mype.Application.ProductCategories;
using Mype.Application.ProductCategories.Commands.CreateProductCategory;
using Mype.Application.ProductCategories.Commands.DeactivateProductCategory;
using Mype.Application.ProductCategories.Commands.ReactivateProductCategory;
using Mype.Application.ProductCategories.Commands.UpdateProductCategory;
using Mype.Application.ProductCategories.Queries.GetProductCategoryDetail;
using Mype.Application.ProductCategories.Queries.ListProductCategories;

namespace Mype.Tests.Api.Endpoints.v1.ProductCategories
{
    public sealed class ProductCategoryEndpointExecutionTests
    {
        private static readonly Guid BusinessId = Guid.NewGuid();
        private static readonly Guid CurrentUserId = Guid.NewGuid();
        private static readonly Guid ProductCategoryId = Guid.NewGuid();

        [Fact]
        public async System.Threading.Tasks.Task Create_Should_Transport_Request_And_Return_Created()
        {
            var senderMock = new Mock<ISender>();
            var userContextProviderMock = CreateUserContextProvider();
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<CreateProductCategoryCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Result<ProductCategoryResult>.Success(CreateResult()));

            var httpResult = await CreateProductCategoryEndpoint.DoAsync(
                BusinessId,
                new CreateProductCategoryEndpoint.Request("Bebidas", null),
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            httpResult
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status201Created);
            senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<CreateProductCategoryCommand>(command =>
                            command.BusinessId == BusinessId
                            && command.CurrentUserId == CurrentUserId
                            && command.Name == "Bebidas"
                            && command.Color == null
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task Update_Should_Transport_Request_And_Return_Ok()
        {
            var senderMock = new Mock<ISender>();
            var userContextProviderMock = CreateUserContextProvider();
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<UpdateProductCategoryCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Result<ProductCategoryResult>.Success(CreateResult()));

            var httpResult = await UpdateProductCategoryEndpoint.DoAsync(
                BusinessId,
                ProductCategoryId,
                new UpdateProductCategoryEndpoint.Request("Bebidas frías", "#3F6FC4", 7),
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            httpResult
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<UpdateProductCategoryCommand>(command =>
                            command.ProductCategoryId == ProductCategoryId
                            && command.Version == 7
                            && command.CurrentUserId == CurrentUserId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task List_Should_Transport_Query_And_Return_Ok()
        {
            var senderMock = new Mock<ISender>();
            var userContextProviderMock = CreateUserContextProvider();
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<ListProductCategoriesQuery>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(
                    Result<IReadOnlyCollection<ProductCategoryResult>>.Success(
                        new[] { CreateResult() }
                    )
                );

            var httpResult = await ListProductCategoriesEndpoint.DoAsync(
                BusinessId,
                "bebidas",
                true,
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            httpResult
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
            senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<ListProductCategoriesQuery>(query =>
                            query.Search == "bebidas"
                            && query.IsActive == true
                            && query.CurrentUserId == CurrentUserId
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task Detail_Should_Transport_Identifiers_And_Return_Ok()
        {
            var senderMock = new Mock<ISender>();
            var userContextProviderMock = CreateUserContextProvider();
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<GetProductCategoryDetailQuery>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Result<ProductCategoryResult>.Success(CreateResult()));

            var httpResult = await GetProductCategoryDetailEndpoint.DoAsync(
                BusinessId,
                ProductCategoryId,
                senderMock.Object,
                userContextProviderMock.Object,
                new DefaultHttpContext(),
                CancellationToken.None
            );

            httpResult
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async System.Threading.Tasks.Task Status_Endpoints_Should_Transport_Version_And_Return_Ok(
            bool deactivate
        )
        {
            var senderMock = new Mock<ISender>();
            var userContextProviderMock = CreateUserContextProvider();
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<DeactivateProductCategoryCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Result<ProductCategoryResult>.Success(CreateResult()));
            senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<ReactivateProductCategoryCommand>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(Result<ProductCategoryResult>.Success(CreateResult()));

            IResult httpResult;
            if (deactivate)
            {
                httpResult = await DeactivateProductCategoryEndpoint.DoAsync(
                    BusinessId,
                    ProductCategoryId,
                    new DeactivateProductCategoryEndpoint.Request(11),
                    senderMock.Object,
                    userContextProviderMock.Object,
                    new DefaultHttpContext(),
                    CancellationToken.None
                );
            }
            else
            {
                httpResult = await ReactivateProductCategoryEndpoint.DoAsync(
                    BusinessId,
                    ProductCategoryId,
                    new ReactivateProductCategoryEndpoint.Request(11),
                    senderMock.Object,
                    userContextProviderMock.Object,
                    new DefaultHttpContext(),
                    CancellationToken.None
                );
            }

            httpResult
                .Should()
                .BeAssignableTo<IStatusCodeHttpResult>()
                .Which.StatusCode.Should()
                .Be(StatusCodes.Status200OK);
        }

        private static Mock<IUserContextProvider> CreateUserContextProvider()
        {
            var userContextProviderMock = new Mock<IUserContextProvider>();
            userContextProviderMock
                .Setup(provider => provider.GetCurrentUserId())
                .Returns(CurrentUserId);
            return userContextProviderMock;
        }

        private static ProductCategoryResult CreateResult()
        {
            return new ProductCategoryResult(
                ProductCategoryId,
                BusinessId,
                "Bebidas",
                "#3F6FC4",
                false,
                true,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                1
            );
        }
    }
}
