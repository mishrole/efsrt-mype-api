using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Mype.Api.Common;
using Mype.Application.Common;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using System;
using System.Collections.Generic;

namespace Mype.Tests.Api.Common
{
    public class HttpErrorMapperTests
    {
        private const string TraceId = "test-trace-id";
        private const string Detail = "test-stack-trace";
        private const string RequiredMessage = "Campo requerido.";
        private const string InvalidMessage = "Campo inválido.";

        [Fact]
        public void FromApplicationError_Should_Map_Conflict()
        {
            var error = new ApplicationError(
                ErrorCodes.EmailAlreadyRegistered,
                ErrorMessages.EmailAlreadyRegistered,
                ApplicationErrorType.Conflict
            );

            var result = HttpErrorMapper.FromApplicationError(
                error,
                TraceId
            );

            result.Code.Should().Be(
                ErrorCodes.EmailAlreadyRegistered
            );

            result.StatusCode.Should().Be(
                StatusCodes.Status409Conflict
            );

            result.Message.Should().Be(
                ErrorMessages.EmailAlreadyRegistered
            );

            result.TraceId.Should().Be(TraceId);
        }

        [Fact]
        public void FromException_Should_Map_ApplicationErrorException()
        {
            var exception = new ApplicationErrorException(
                ErrorCodes.EmailAlreadyRegistered,
                ErrorMessages.EmailAlreadyRegistered,
                ApplicationErrorType.Conflict
            );

            var result = HttpErrorMapper.FromException(
                exception,
                TraceId,
                Detail
            );

            result.Code.Should().Be(
                ErrorCodes.EmailAlreadyRegistered
            );

            result.StatusCode.Should().Be(
                StatusCodes.Status409Conflict
            );

            result.Message.Should().Be(
                ErrorMessages.EmailAlreadyRegistered
            );

            result.Detail.Should().Be(Detail);
            result.TraceId.Should().Be(TraceId);
        }

        [Fact]
        public void FromException_Should_Group_Validation_Errors_By_Property()
        {
            var exception = new ValidationException(
                new List<ValidationFailure>
                {
                    new("Email", RequiredMessage),
                    new("Email", InvalidMessage),
                    new("Password", RequiredMessage)
                }
            );

            var result = HttpErrorMapper.FromException(
                exception,
                TraceId,
                Detail
            );

            result.Code.Should().Be(
                ErrorCodes.ValidationError
            );

            result.StatusCode.Should().Be(
                StatusCodes.Status400BadRequest
            );

            result.Message.Should().Be(
                ErrorMessages.ValidationFailed
            );

            result.Errors.Should().ContainKey("Email");

            result.Errors["Email"].Should().BeEquivalentTo(
                [
                    RequiredMessage,
                    InvalidMessage
                ]
            );

            result.Errors.Should().ContainKey("Password");

            result.Errors["Password"]
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Be(RequiredMessage);

            result.Detail.Should().Be(Detail);
            result.TraceId.Should().Be(TraceId);
        }

        [Fact]
        public void FromException_Should_Remove_Duplicate_Validation_Errors()
        {
            var exception = new ValidationException(
                new List<ValidationFailure>
                {
                    new("Email", RequiredMessage),
                    new("Email", RequiredMessage)
                }
            );

            var result = HttpErrorMapper.FromException(
                exception,
                TraceId,
                Detail
            );

            result.Errors["Email"]
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Be(RequiredMessage);
        }

        [Fact]
        public void FromException_Should_Return_Safe_Internal_Error_When_Exception_Is_Unknown()
        {
            var exception = new Exception(
                "Sensitive internal error."
            );

            var result = HttpErrorMapper.FromException(
                exception,
                TraceId,
                Detail
            );

            result.Code.Should().Be(
                ErrorCodes.InternalError
            );

            result.StatusCode.Should().Be(
                StatusCodes.Status500InternalServerError
            );

            result.Message.Should().Be(
                ErrorMessages.InternalError
            );

            result.Message.Should().NotContain(
                exception.Message
            );

            result.Detail.Should().Be(Detail);
            result.TraceId.Should().Be(TraceId);
        }

        [Theory]
        [InlineData(
            typeof(ArgumentNullException),
            StatusCodes.Status400BadRequest
        )]
        [InlineData(
            typeof(ArgumentException),
            StatusCodes.Status400BadRequest
        )]
        [InlineData(
            typeof(UnauthorizedAccessException),
            StatusCodes.Status401Unauthorized
        )]
        [InlineData(
            typeof(KeyNotFoundException),
            StatusCodes.Status404NotFound
        )]
        [InlineData(
            typeof(InvalidOperationException),
            StatusCodes.Status409Conflict
        )]
        public void FromException_Should_Map_Known_Exception_Status(
            Type exceptionType,
            int expectedStatusCode
        )
        {
            var exception = (Exception)Activator.CreateInstance(
                exceptionType,
                "Known error."
            )!;

            var result = HttpErrorMapper.FromException(
                exception,
                TraceId,
                Detail
            );

            result.StatusCode.Should().Be(expectedStatusCode);
            result.Message.Should().Be(exception.Message);
            result.TraceId.Should().Be(TraceId);
        }
    }
}