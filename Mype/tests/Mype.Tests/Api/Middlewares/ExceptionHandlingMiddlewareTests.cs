using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;
using Mype.Api.Middlewares;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mype.Tests.Api.Middlewares
{
    public class ExceptionHandlingMiddlewareTests
    {
        private const string TraceId =
            "test-trace-id";

        [Fact]
        public async Task InvokeAsync_Should_Return_Validation_Error_For_BadHttpRequest()
        {
            var middleware =
                new ExceptionHandlingMiddleware(
                    _ => throw new
                        BadHttpRequestException(
                            "Sensitive binding detail."
                        ),
                    Mock.Of<
                        ILogger<
                            ExceptionHandlingMiddleware
                        >
                    >(),
                    new TestWebHostEnvironment()
                );

            var context =
                new DefaultHttpContext
                {
                    TraceIdentifier = TraceId
                };

            context.Response.Body =
                new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be(
                StatusCodes.Status400BadRequest
            );

            context.Response.Body.Position = 0;

            var response =
                await JsonSerializer
                    .DeserializeAsync<
                        HttpStatusCodeInfo
                    >(
                        context.Response.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive =
                                true
                        }
                    );

            response.Should().NotBeNull();
            response.Code.Should().Be(
                ErrorCodes.ValidationError
            );
            response.Message.Should().Be(
                ErrorMessages.ValidationFailed
            );
            response.Detail.Should().BeNull();
            response.TraceId.Should().Be(TraceId);
        }

        private sealed class
            TestWebHostEnvironment
            : IWebHostEnvironment
        {
            public string ApplicationName
            {
                get;
                set;
            } = string.Empty;

            public IFileProvider WebRootFileProvider
            {
                get;
                set;
            } = new NullFileProvider();

            public string WebRootPath
            {
                get;
                set;
            } = string.Empty;

            public string EnvironmentName
            {
                get;
                set;
            } = "Development";

            public string ContentRootPath
            {
                get;
                set;
            } = string.Empty;

            public IFileProvider
                ContentRootFileProvider
            {
                get;
                set;
            } = new NullFileProvider();
        }
    }
}
