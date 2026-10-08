using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;

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
            WriteIndented = false
        };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format(ErrorMessages.ExceptionHandlingMiddlewareError, ex.Message));
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = exception switch
            {
                ApplicationErrorException { ErrorType: ApplicationErrorType.Validation } => StatusCodes.Status400BadRequest,
                ApplicationErrorException { ErrorType: ApplicationErrorType.Unauthorized } => StatusCodes.Status401Unauthorized,
                ApplicationErrorException { ErrorType: ApplicationErrorType.Forbidden } => StatusCodes.Status403Forbidden,
                ApplicationErrorException { ErrorType: ApplicationErrorType.NotFound } => StatusCodes.Status404NotFound,
                ApplicationErrorException { ErrorType: ApplicationErrorType.Conflict } => StatusCodes.Status409Conflict,

                ArgumentNullException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                InvalidOperationException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            var code = exception is ApplicationErrorException appErrorException ? appErrorException.Code : ErrorCodes.InternalError;

            var response = new HttpStatusCodeInfo
            {
                Code = code,
                StatusCode = statusCode,
                Message = statusCode == StatusCodes.Status500InternalServerError ? ErrorMessages.InternalError : exception.Message,
                Detail = _env.IsDevelopment() ? exception.StackTrace : null,
                TraceId = context.TraceIdentifier
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
        }

    }
}
