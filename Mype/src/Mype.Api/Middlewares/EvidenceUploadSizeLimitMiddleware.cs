using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Mype.Application.MovementEvidences.Interfaces;
using Mype.Shared.Constants;
using Mype.Shared.Models;

namespace Mype.Api.Middlewares
{
    public sealed class EvidenceUploadSizeLimitMiddleware
    {
        private const long MultipartOverheadBytes = 64 * 1024;
        private readonly RequestDelegate _next;
        private readonly long _maximumRequestBodySize;

        public EvidenceUploadSizeLimitMiddleware(
            RequestDelegate next,
            IEvidenceStorageSettings settings
        )
        {
            _next = next;
            _maximumRequestBodySize = checked(settings.MaxSizeBytes + MultipartOverheadBytes);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsEvidenceUpload(context.Request))
            {
                await _next(context);
                return;
            }

            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (feature is { IsReadOnly: false })
                feature.MaxRequestBodySize = _maximumRequestBodySize;

            if (context.Request.ContentLength > _maximumRequestBodySize)
            {
                var response = new HttpStatusCodeInfo
                {
                    Code = ErrorCodes.InvalidEvidenceSize,
                    StatusCode = StatusCodes.Status413PayloadTooLarge,
                    Message = ErrorMessages.InvalidEvidenceSize,
                    TraceId = context.TraceIdentifier,
                };

                context.Response.StatusCode = response.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
                return;
            }

            await _next(context);
        }

        private static bool IsEvidenceUpload(HttpRequest request) =>
            (HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method))
            && request.Path.StartsWithSegments(
                "/api/v1/businesses",
                StringComparison.OrdinalIgnoreCase
            )
            && request.Path.Value?.Contains(
                "/financial-movements/",
                StringComparison.OrdinalIgnoreCase
            ) == true
            && request.Path.Value.Contains("/evidences", StringComparison.OrdinalIgnoreCase);
    }
}
