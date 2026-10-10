using FluentValidation;
using Microsoft.AspNetCore.Http;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Mype.Api.Common
{
    public static class HttpErrorMapper
    {
        public static HttpStatusCodeInfo FromApplicationError(
            ApplicationError error,
            string traceId
        )
        {
            return FromApplicationError(
                error.Code,
                error.Message,
                error.Type,
                traceId
            );
        }

        public static HttpStatusCodeInfo FromApplicationError(
            string code,
            string message,
            ApplicationErrorType errorType,
            string traceId,
            string detail = null,
            Dictionary<string, string[]> errors = null
        )
        {
            return new HttpStatusCodeInfo
            {
                Code = code,
                StatusCode = GetStatusCode(
                    errorType
                ),
                Message = message,
                Detail = detail,
                TraceId = traceId,
                Errors = errors ?? []
            };
        }

        public static HttpStatusCodeInfo FromException(
            Exception exception,
            string traceId,
            string detail
        )
        {
            if (exception is ApplicationErrorException applicationError)
            {
                var response = FromApplicationError(
                    applicationError.Code,
                    applicationError.Message,
                    applicationError.ErrorType,
                    traceId,
                    detail
                );

                return response;
            }

            if (exception is ValidationException validationException)
            {
                var errors = validationException.Errors
                    .GroupBy(error =>
                        error.PropertyName
                    )
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error =>
                                error.ErrorMessage
                            )
                            .Distinct()
                            .ToArray()
                    );

                return FromApplicationError(
                    ErrorCodes.ValidationError,
                    ErrorMessages.ValidationFailed,
                    ApplicationErrorType.Validation,
                    traceId,
                    errors: errors
                );
            }

            if (exception is BadHttpRequestException badHttpRequestException)
            {
                var errors =
                    CreateBindingErrors(
                        badHttpRequestException
                    );

                return FromApplicationError(
                    ErrorCodes.ValidationError,
                    ErrorMessages.ValidationFailed,
                    ApplicationErrorType.Validation,
                    traceId,
                    errors: errors
                );
            }

            var statusCode = exception switch
            {
                ArgumentNullException =>
                    StatusCodes.Status400BadRequest,

                ArgumentException =>
                    StatusCodes.Status400BadRequest,

                UnauthorizedAccessException =>
                    StatusCodes.Status401Unauthorized,

                KeyNotFoundException =>
                    StatusCodes.Status404NotFound,

                InvalidOperationException =>
                    StatusCodes.Status409Conflict,

                _ =>
                    StatusCodes.Status500InternalServerError
            };

            return new HttpStatusCodeInfo
            {
                Code = ErrorCodes.InternalError,
                StatusCode = statusCode,
                Message = statusCode == StatusCodes.Status500InternalServerError
                    ? ErrorMessages.InternalError
                    : exception.Message,
                Detail = detail,
                TraceId = traceId
            };
        }

        private static int GetStatusCode(
            ApplicationErrorType errorType
        )
        {
            return errorType switch
            {
                ApplicationErrorType.Validation =>
                    StatusCodes.Status400BadRequest,

                ApplicationErrorType.Unauthorized =>
                    StatusCodes.Status401Unauthorized,

                ApplicationErrorType.Forbidden =>
                    StatusCodes.Status403Forbidden,

                ApplicationErrorType.NotFound =>
                    StatusCodes.Status404NotFound,

                ApplicationErrorType.Conflict =>
                    StatusCodes.Status409Conflict,

                ApplicationErrorType.UnprocessableEntity =>
                    StatusCodes.Status422UnprocessableEntity,

                ApplicationErrorType.Internal =>
                    StatusCodes.Status500InternalServerError,

                _ =>
                    StatusCodes.Status500InternalServerError
            };
        }

        private static Dictionary<string, string[]>
            CreateBindingErrors(
                BadHttpRequestException exception
            )
        {
            if (
                exception.InnerException is not
                    JsonException jsonException ||
                string.IsNullOrWhiteSpace(
                    jsonException.Path
                )
            )
            {
                return [];
            }

            var propertyName = jsonException.Path
                .TrimStart('$', '.');

            if (
                string.IsNullOrWhiteSpace(
                    propertyName
                )
            )
            {
                return [];
            }

            return new Dictionary<string, string[]>
            {
                {
                    propertyName,
                    [
                        ValidationMessages.Invalid
                            .Replace(
                                "{PropertyName}",
                                propertyName
                            )
                    ]
                }
            };
        }
    }
}