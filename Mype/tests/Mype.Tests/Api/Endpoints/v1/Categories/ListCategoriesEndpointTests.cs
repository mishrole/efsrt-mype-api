using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Categories;
using Mype.Application.Categories.Queries.ListCategories;
using Mype.Application.Common;
using Mype.Domain.Categories;
using Mype.Shared.Constants;
using Mype.Shared.Models;

namespace Mype.Tests.Api.Endpoints.v1.Categories
{
    public class ListCategoriesEndpointTests
    {
        private const string CategoryName = "Productos";

        private const string TraceId = "test-trace-id";

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid CategoryId = Guid.NewGuid();

        private readonly Mock<ISender> _senderMock = new();

        private readonly Mock<IUserContextProvider> _userContextProviderMock = new();

        public ListCategoriesEndpointTests()
        {
            _userContextProviderMock
                .Setup(provider => provider.GetCurrentUserId())
                .Returns(CurrentUserId);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Ok_With_Categories()
        {
            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Success(
                    CreateApplicationResult()
                )
            );

            var result = await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                null,
                null,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult
                .Value.Should()
                .BeAssignableTo<IReadOnlyCollection<CategoryListItemResult>>()
                .Subject;

            response.Should().ContainSingle();

            response
                .Should()
                .Contain(category =>
                    category.Id == CategoryId
                    && category.BusinessId == BusinessId
                    && category.Name == CategoryName
                    && category.Type == CategoryType.Sale
                    && category.IsDefault
                    && category.IsActive
                );

            VerifyQueryWasSent(null, null);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Ok_With_Empty_Collection()
        {
            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Success(
                    Array.Empty<CategoryListItemResult>()
                )
            );

            var result = await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                null,
                null,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult
                .Value.Should()
                .BeAssignableTo<IReadOnlyCollection<CategoryListItemResult>>()
                .Subject;

            response.Should().BeEmpty();
        }

        [Fact]
        public async Task DoAsync_Should_Create_Query_With_Business_User_And_Filters()
        {
            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Success(
                    Array.Empty<CategoryListItemResult>()
                )
            );

            await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                CategoryType.Expense,
                false,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            _userContextProviderMock.Verify(provider => provider.GetCurrentUserId(), Times.Once);

            VerifyQueryWasSent(CategoryType.Expense, false);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Forbidden_When_Business_Access_Is_Denied()
        {
            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Failure(
                    ListCategoriesErrors.BusinessAccessForbidden
                )
            );

            var result = await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                null,
                null,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            AssertErrorResponse(
                result,
                StatusCodes.Status403Forbidden,
                ErrorCodes.BusinessAccessForbidden,
                ErrorMessages.BusinessAccessForbidden
            );
        }

        [Fact]
        public async Task DoAsync_Should_Return_Forbidden_When_Category_Read_Is_Missing()
        {
            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Failure(
                    ListCategoriesErrors.CategoryAccessForbidden
                )
            );

            var result = await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                null,
                null,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            AssertErrorResponse(
                result,
                StatusCodes.Status403Forbidden,
                ErrorCodes.CategoryAccessForbidden,
                ErrorMessages.CategoryAccessForbidden
            );
        }

        [Fact]
        public async Task DoAsync_Should_Return_Conflict_When_Business_Is_Unavailable()
        {
            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Failure(
                    ListCategoriesErrors.BusinessUnavailable
                )
            );

            var result = await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                null,
                null,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            AssertErrorResponse(
                result,
                StatusCodes.Status409Conflict,
                ErrorCodes.BusinessUnavailable,
                ErrorMessages.BusinessUnavailable
            );
        }

        [Fact]
        public async Task DoAsync_Should_Forward_CancellationToken()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = cancellationTokenSource.Token;

            SetupSender(
                Result<IReadOnlyCollection<CategoryListItemResult>>.Success(
                    Array.Empty<CategoryListItemResult>()
                ),
                cancellationToken
            );

            await ListCategoriesEndpoint.DoAsync(
                BusinessId,
                null,
                null,
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                cancellationToken
            );

            _senderMock.Verify(
                sender => sender.Send(It.IsAny<ListCategoriesQuery>(), cancellationToken),
                Times.Once
            );
        }

        private void SetupSender(
            Result<IReadOnlyCollection<CategoryListItemResult>> result,
            CancellationToken? cancellationToken = null
        )
        {
            _senderMock
                .Setup(sender =>
                    sender.Send(
                        It.IsAny<ListCategoriesQuery>(),
                        cancellationToken.HasValue
                            ? cancellationToken.Value
                            : It.IsAny<CancellationToken>()
                    )
                )
                .ReturnsAsync(result);
        }

        private void VerifyQueryWasSent(CategoryType? type, bool? isActive)
        {
            _senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<ListCategoriesQuery>(query =>
                            query.BusinessId == BusinessId
                            && query.CurrentUserId == CurrentUserId
                            && query.Type == type
                            && query.IsActive == isActive
                        ),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        private static void AssertErrorResponse(
            IResult result,
            int expectedStatusCode,
            string expectedCode,
            string expectedMessage
        )
        {
            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(expectedStatusCode);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult.Value.Should().BeOfType<HttpStatusCodeInfo>().Subject;

            response.Code.Should().Be(expectedCode);

            response.StatusCode.Should().Be(expectedStatusCode);

            response.Message.Should().Be(expectedMessage);

            response.TraceId.Should().Be(TraceId);
        }

        private static IReadOnlyCollection<CategoryListItemResult> CreateApplicationResult()
        {
            return
            [
                new CategoryListItemResult(
                    CategoryId,
                    BusinessId,
                    CategoryName,
                    CategoryType.Sale,
                    true,
                    true
                ),
            ];
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext { TraceIdentifier = TraceId };
        }
    }
}
