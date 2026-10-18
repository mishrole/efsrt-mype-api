using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mype.Api.Common;
using Mype.Shared.Constants;

namespace Mype.Api.Middlewares
{
    [ExcludeFromCodeCoverage]
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment env
        )
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug(
                    ErrorMessages.ExceptionHandlingMiddlewareRequestAbortedError,
                    context.TraceIdentifier
                );
            }
            catch (BadHttpRequestException exception)
            {
                _logger.LogWarning(exception, ErrorMessages.BadHttpRequestReceived);
                await HandleExceptionAsync(context, exception);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    ErrorMessages.ExceptionHandlingMiddlewareError,
                    exception.Message
                );
                await HandleExceptionAsync(context, exception);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted || context.RequestAborted.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var detail = _env.IsDevelopment() ? exception.StackTrace : null;

            var response = HttpErrorMapper.FromException(
                exception,
                context.TraceIdentifier,
                detail
            );

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = response.StatusCode;

            var serializedResponse = JsonSerializer.Serialize(response, _jsonOptions);

            return context.Response.WriteAsync(serializedResponse, CancellationToken.None);
        }
    }
}
