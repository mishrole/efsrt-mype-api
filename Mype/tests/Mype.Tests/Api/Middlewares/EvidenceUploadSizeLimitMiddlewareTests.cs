using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Mype.Api.Middlewares;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Shared.Constants;
using Mype.Shared.Models;

namespace Mype.Tests.Api.Middlewares
{
    public sealed class EvidenceUploadSizeLimitMiddlewareTests
    {
        private const long MaxFileSize = 100;
        private const long MaximumRequestSize = MaxFileSize + (64 * 1024);

        [Fact]
        public async Task InvokeAsync_Should_Reject_Oversized_Evidence_Request_Before_Next()
        {
            var nextCalled = false;
            var middleware = new EvidenceUploadSizeLimitMiddleware(
                _ =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                new Settings()
            );
            var context = CreateContext(HttpMethods.Post, MaximumRequestSize + 1);

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeFalse();
            context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
            context.Response.Body.Position = 0;
            var response = await JsonSerializer.DeserializeAsync<HttpStatusCodeInfo>(
                context.Response.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            response.Code.Should().Be(ErrorCodes.InvalidEvidenceSize);
        }

        [Fact]
        public async Task InvokeAsync_Should_Set_Server_Limit_And_Continue_For_Valid_Request()
        {
            var nextCalled = false;
            var middleware = new EvidenceUploadSizeLimitMiddleware(
                _ =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                new Settings()
            );
            var context = CreateContext(HttpMethods.Put, MaximumRequestSize);
            var feature = new MutableMaximumBodySizeFeature();
            context.Features.Set<IHttpMaxRequestBodySizeFeature>(feature);

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeTrue();
            feature.MaxRequestBodySize.Should().Be(MaximumRequestSize);
        }

        [Theory]
        [InlineData("GET", "/api/v1/businesses/00000000-0000-0000-0000-000000000001/financial-movements/00000000-0000-0000-0000-000000000002/evidences")]
        [InlineData("POST", "/api/v1/businesses/00000000-0000-0000-0000-000000000001/financial-movements/00000000-0000-0000-0000-000000000002/sale-items")]
        public async Task InvokeAsync_Should_Ignore_NonUpload_Request(string method, string path)
        {
            var nextCalled = false;
            var middleware = new EvidenceUploadSizeLimitMiddleware(
                _ =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                new Settings()
            );
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;
            context.Request.ContentLength = MaximumRequestSize + 1;

            await middleware.InvokeAsync(context);

            nextCalled.Should().BeTrue();
        }

        private static DefaultHttpContext CreateContext(string method, long contentLength)
        {
            var context = new DefaultHttpContext { TraceIdentifier = "trace" };
            context.Request.Method = method;
            context.Request.Path = "/api/v1/businesses/00000000-0000-0000-0000-000000000001/financial-movements/00000000-0000-0000-0000-000000000002/evidences";
            context.Request.ContentLength = contentLength;
            context.Response.Body = new MemoryStream();
            return context;
        }

        private sealed class Settings : IEvidenceStorageSettings
        {
            public string Provider => "AzureBlob";
            public string ContainerName => "evidences";
            public long MaxSizeBytes => MaxFileSize;
            public int ReadUriMinutes => 5;
            public int PurgeRetentionDays => 30;
        }

        private sealed class MutableMaximumBodySizeFeature : IHttpMaxRequestBodySizeFeature
        {
            public bool IsReadOnly => false;
            public long? MaxRequestBodySize { get; set; }
        }
    }
}
