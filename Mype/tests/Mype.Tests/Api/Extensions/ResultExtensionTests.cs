using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mype.Api.Extensions;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mype.Tests.Api.Extensions
{
    public class ResultExtensionTests
    {
        private const string TraceId = "test-trace-id";

        public sealed class TestResult
        {
            public int Id { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void ToHttpResult_Should_Return_Success_Result_When_Result_Is_Successful()
        {
            var value = new TestResult
            {
                Id = 1
            };

            var result = Result<TestResult>.Success(value);
            var context = CreateHttpContext();
            var expectedHttpResult = Results.Created(
                "/api/v1/test/1",
                value
            );

            var httpResult = result.ToHttpResult(
                context,
                returnedValue =>
                {
                    returnedValue.Should().BeSameAs(value);

                    return expectedHttpResult;
                }
            );

            httpResult.Should().BeSameAs(expectedHttpResult);
        }

        [Fact]
        public async Task ToHttpResult_Should_Return_Conflict_Body_When_Result_Is_Failure()
        {
            var error = new ApplicationError(
                ErrorCodes.EmailAlreadyRegistered,
                ErrorMessages.EmailAlreadyRegistered,
                ApplicationErrorType.Conflict
            );

            var result = Result<TestResult>.Failure(error);
            var context = CreateHttpContext();

            var httpResult = result.ToHttpResult(
                context,
                _ => Results.NoContent()
            );

            await httpResult.ExecuteAsync(context);

            context.Response.StatusCode.Should().Be(
                StatusCodes.Status409Conflict
            );

            context.Response.Body.Position = 0;

            var response = await JsonSerializer.DeserializeAsync<
                HttpStatusCodeInfo
            >(
                context.Response.Body,
                JsonOptions
            );

            response.Should().NotBeNull();

            response.Code.Should().Be(
                ErrorCodes.EmailAlreadyRegistered
            );

            response.StatusCode.Should().Be(
                StatusCodes.Status409Conflict
            );

            response.Message.Should().Be(
                ErrorMessages.EmailAlreadyRegistered
            );

            response.TraceId.Should().Be(TraceId);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddOptions()
                .BuildServiceProvider();

            return new DefaultHttpContext
            {
                TraceIdentifier = TraceId,
                RequestServices = services,
                Response =
                {
                    Body = new MemoryStream()
                }
            };
        }
    }
}