using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Mype.Api.Middlewares;
using Mype.Shared.Constants;
using Mype.Shared.Models;

namespace Mype.Tests.Api.Middlewares
{
    public class AuthenticationHandlingMiddlewareTests
    {
        private const string TraceId = "test-trace-id";

        [Fact]
        public async Task InvokeAsync_Should_Write_Unauthenticated_Response_When_Status_Is_401_And_Body_Is_Empty()
        {
            var context = CreateHttpContext();

            var middleware = new AuthenticationHandlingMiddleware(currentContext =>
            {
                currentContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            var response = await ReadResponseAsync(context);

            context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            context.Response.ContentType.Should().StartWith("application/json");

            response.Code.Should().Be(ErrorCodes.Unauthenticated);

            response.Message.Should().Be(ErrorMessages.Unauthenticated);

            response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            response.TraceId.Should().Be(TraceId);
        }

        [Fact]
        public async Task InvokeAsync_Should_Not_Write_Response_When_Status_Is_Not_401()
        {
            var context = CreateHttpContext();

            var middleware = new AuthenticationHandlingMiddleware(currentContext =>
            {
                currentContext.Response.StatusCode = StatusCodes.Status200OK;

                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            context.Response.Body.Length.Should().Be(0);
        }

        [Fact]
        public async Task InvokeAsync_Should_Not_Overwrite_Existing_401_Response()
        {
            var context = CreateHttpContext();

            var middleware = new AuthenticationHandlingMiddleware(async currentContext =>
            {
                currentContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

                currentContext.Response.ContentLength = 8;

                await currentContext.Response.WriteAsync("existing", currentContext.RequestAborted);
            });

            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;

            using var reader = new StreamReader(context.Response.Body, leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            body.Should().Be("existing");
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext { TraceIdentifier = TraceId };

            context.Response.Body = new MemoryStream();

            return context;
        }

        private static async Task<HttpStatusCodeInfo> ReadResponseAsync(HttpContext context)
        {
            context.Response.Body.Position = 0;

            var response = await JsonSerializer.DeserializeAsync<HttpStatusCodeInfo>(
                context.Response.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return response;
        }
    }
}
