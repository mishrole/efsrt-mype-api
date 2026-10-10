using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Mype.Api.Context;
using Mype.Api.Endpoints.v1.Businesses;
using Mype.Application.Businesses.Commands.CreateBusiness;
using Mype.Application.Common;
using Mype.Domain.Categories;
using Mype.Shared.Constants;
using Mype.Shared.Models;

namespace Mype.Tests.Api.Endpoints.v1.Businesses
{
    public class CreateBusinessEndpointTests
    {
        private const string DisplayName = "Bodega Central";

        private const string LegalName = "Comercial Central E.I.R.L.";

        private const string Ruc = "20123456786";

        private const string CurrencyCode = "PEN";

        private const string RoleCode = "OWNER";

        private const string TraceId = "test-trace-id";

        private static readonly Guid CurrentUserId = Guid.NewGuid();

        private static readonly Guid BusinessId = Guid.NewGuid();

        private static readonly Guid MembershipId = Guid.NewGuid();

        private readonly Mock<ISender> _senderMock = new();

        private readonly Mock<IUserContextProvider> _userContextProviderMock = new();

        public CreateBusinessEndpointTests()
        {
            _userContextProviderMock
                .Setup(provider => provider.GetCurrentUserId())
                .Returns(CurrentUserId);
        }

        [Fact]
        public async Task DoAsync_Should_Return_Created_When_Business_Creation_Succeeds()
        {
            SetupSender(Result<CreateBusinessResult>.Success(CreateApplicationResult()));

            var context = CreateHttpContext();

            var result = await CreateBusinessEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                _userContextProviderMock.Object,
                context,
                CancellationToken.None
            );

            var statusResult = result.Should().BeAssignableTo<IStatusCodeHttpResult>().Subject;

            statusResult.StatusCode.Should().Be(StatusCodes.Status201Created);

            var valueResult = result.Should().BeAssignableTo<IValueHttpResult>().Subject;

            var response = valueResult.Value.Should().BeOfType<CreateBusinessResult>().Subject;

            response.BusinessId.Should().Be(BusinessId);

            response.DisplayName.Should().Be(DisplayName);

            response.CurrencyCode.Should().Be(CurrencyCode);

            response.MembershipId.Should().Be(MembershipId);

            response.RoleCode.Should().Be(RoleCode);

            response.DefaultCategories.Should().ContainSingle();

            _userContextProviderMock.Verify(provider => provider.GetCurrentUserId(), Times.Once);

            VerifyCommandWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Create_Command_With_Request_And_Current_User()
        {
            SetupSender(Result<CreateBusinessResult>.Success(CreateApplicationResult()));

            await CreateBusinessEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            VerifyCommandWasSent();
        }

        [Fact]
        public async Task DoAsync_Should_Return_UnprocessableEntity_When_Currency_Is_Unsupported()
        {
            SetupSender(
                Result<CreateBusinessResult>.Failure(CreateBusinessErrors.UnsupportedCurrency)
            );

            var result = await CreateBusinessEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            AssertErrorResponse(
                result,
                StatusCodes.Status422UnprocessableEntity,
                ErrorCodes.UnsupportedCurrency,
                ErrorMessages.UnsupportedCurrency
            );
        }

        [Fact]
        public async Task DoAsync_Should_Return_InternalServerError_When_Owner_Role_Is_Unavailable()
        {
            SetupSender(
                Result<CreateBusinessResult>.Failure(CreateBusinessErrors.SystemRoleUnavailable)
            );

            var result = await CreateBusinessEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            AssertErrorResponse(
                result,
                StatusCodes.Status500InternalServerError,
                ErrorCodes.SystemRoleUnavailable,
                ErrorMessages.SystemRoleUnavailable
            );
        }

        [Fact]
        public async Task DoAsync_Should_Return_InternalServerError_When_Creation_Fails()
        {
            SetupSender(
                Result<CreateBusinessResult>.Failure(CreateBusinessErrors.BusinessCreationFailed)
            );

            var result = await CreateBusinessEndpoint.DoAsync(
                CreateRequest(),
                _senderMock.Object,
                _userContextProviderMock.Object,
                CreateHttpContext(),
                CancellationToken.None
            );

            AssertErrorResponse(
                result,
                StatusCodes.Status500InternalServerError,
                ErrorCodes.BusinessCreationFailed,
                ErrorMessages.BusinessCreationFailed
            );
        }

        private void SetupSender(Result<CreateBusinessResult> result)
        {
            _senderMock
                .Setup(sender =>
                    sender.Send(It.IsAny<CreateBusinessCommand>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(result);
        }

        private void VerifyCommandWasSent()
        {
            _senderMock.Verify(
                sender =>
                    sender.Send(
                        It.Is<CreateBusinessCommand>(command =>
                            command.DisplayName == DisplayName
                            && command.LegalName == LegalName
                            && command.Ruc == Ruc
                            && command.CurrencyCode == CurrencyCode
                            && command.CurrentUserId == CurrentUserId
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

            response.Message.Should().Be(expectedMessage);

            response.StatusCode.Should().Be(expectedStatusCode);

            response.TraceId.Should().Be(TraceId);
        }

        private static CreateBusinessEndpoint.CreateBusinessRequest CreateRequest()
        {
            return new CreateBusinessEndpoint.CreateBusinessRequest(
                DisplayName,
                LegalName,
                Ruc,
                CurrencyCode
            );
        }

        private static CreateBusinessResult CreateApplicationResult()
        {
            return new CreateBusinessResult
            {
                BusinessId = BusinessId,
                DisplayName = DisplayName,
                CurrencyCode = CurrencyCode,
                MembershipId = MembershipId,
                RoleCode = RoleCode,
                Version = 1,
                DefaultCategories =
                [
                    new CategoryResult
                    {
                        Id = Guid.NewGuid(),
                        Name = "Productos",
                        Type = CategoryType.Sale,
                        IsDefault = true,
                        IsActive = true,
                    },
                ],
            };
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext { TraceIdentifier = TraceId };
        }
    }
}
