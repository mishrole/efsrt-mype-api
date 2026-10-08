using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mype.Application.Common.Exceptions;
using Mype.Infrastructure.Persistence.Exceptions;
using Mype.Shared.Constants;
using Npgsql;
using System;

namespace Mype.Tests.Infrastructure.Persistence.Exceptions
{
    public class PostgreSqlExceptionTranslatorTests
    {
        private readonly PostgreSqlExceptionTranslator _translator = new();

        [Fact]
        public void Translate_Should_Return_ApplicationErrorException_When_NormalizedEmail_Is_Duplicated()
        {
            var exception = CreateDbUpdateException(
                PostgresErrorCodes.UniqueViolation,
                DatabaseConstraints.UsersNormalizedEmail
            );

            var result = _translator.Translate(exception);

            result.Should().BeOfType<ApplicationErrorException>();
        }

        [Fact]
        public void Translate_Should_Return_EmailAlreadyRegistered_Code_When_NormalizedEmail_Is_Duplicated()
        {
            var exception = CreateDbUpdateException(
                PostgresErrorCodes.UniqueViolation,
                DatabaseConstraints.UsersNormalizedEmail
            );

            var result = _translator.Translate(exception);

            var applicationException = result
                .Should()
                .BeOfType<ApplicationErrorException>()
                .Subject;

            applicationException.Code.Should().Be(
                ErrorCodes.EmailAlreadyRegistered
            );

            applicationException.Message.Should().Be(
                ErrorMessages.EmailAlreadyRegistered
            );

            applicationException.ErrorType.Should().Be(
                ApplicationErrorType.Conflict
            );

            applicationException.InnerException.Should().BeSameAs(
                exception
            );
        }

        [Fact]
        public void Translate_Should_Return_Original_Exception_When_Unique_Constraint_Is_Unknown()
        {
            var exception = CreateDbUpdateException(
                PostgresErrorCodes.UniqueViolation,
                DatabaseConstraints.UnknownConstraint
            );

            var result = _translator.Translate(exception);

            result.Should().BeSameAs(exception);
        }

        [Fact]
        public void Translate_Should_Return_Original_Exception_When_Postgres_Error_Is_Not_Unique_Violation()
        {
            var exception = CreateDbUpdateException(
                PostgresErrorCodes.ForeignKeyViolation,
                DatabaseConstraints.UsersNormalizedEmail
            );

            var result = _translator.Translate(exception);

            result.Should().BeSameAs(exception);
        }

        [Fact]
        public void Translate_Should_Return_Original_Exception_When_Exception_Is_Not_DbUpdateException()
        {
            var exception = new InvalidOperationException(
                ErrorMessages.UnexpectedError
            );

            var result = _translator.Translate(exception);

            result.Should().BeSameAs(exception);
        }

        private static DbUpdateException CreateDbUpdateException(
            string sqlState,
            string constraintName
        )
        {
            var postgresException = new PostgresException(
                messageText: "Database operation failed.",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: sqlState,
                detail: null,
                hint: null,
                position: 0,
                internalPosition: 0,
                internalQuery: null,
                where: null,
                schemaName: "public",
                tableName: "users",
                columnName: "normalized_email",
                dataTypeName: null,
                constraintName: constraintName,
                file: null,
                line: null,
                routine: null
            );

            return new DbUpdateException(
                ErrorMessages.SaveChangesError,
                postgresException
            );
        }
    }
}
