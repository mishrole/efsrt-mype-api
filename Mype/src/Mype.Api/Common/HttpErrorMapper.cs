using FluentValidation;
using Microsoft.AspNetCore.Http;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using Mype.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mype.Api.Common
{
    public static class HttpErrorMapper
    {
        public static HttpStatusCodeInfo FromApplicationError(
            string code,
            string message,
            ApplicationErrorType errorType,
            string traceId
        )
        {
            return new HttpStatusCodeInfo
            {
                Code = code,
                StatusCode = GetStatusCode(errorType),
                Message = message,
                TraceId = traceId
            };
        }

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
                    traceId
                );

                response.Detail = detail;

                return response;
            }

            if (exception is ValidationException validationException)
            {
                return new HttpStatusCodeInfo
                {
                    Code = ErrorCodes.ValidationError,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = ErrorMessages.ValidationFailed,
                    Detail = detail,
                    TraceId = traceId,
                    Errors = validationException.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .Select(error => error.ErrorMessage)
                                .Distinct()
                                .ToArray()
                        )
                };
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
    }
}