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

        [Theory]
        [InlineData("ux_users_normalized_email", "EMAIL_ALREADY_REGISTERED")]
        [InlineData("ux_products_business_name", "PRODUCT_ALREADY_EXISTS")]
        public void Translate_Should_Map_Known_Unique_Constraints(
            string constraint,
            string expected
        )
        {
            var result = _translator.Translate(
                CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, constraint)
            );
            result.Should().BeOfType<ApplicationErrorException>().Which.Code.Should().Be(expected);
        }

        [Fact]
        public void Translate_Should_Map_Concurrency_Conflict()
        {
            var exception = new DbUpdateConcurrencyException("conflict");
            var result = _translator.Translate(exception);
            result
                .Should()
                .BeOfType<ApplicationErrorException>()
                .Which.Code.Should()
                .Be(ErrorCodes.ConcurrencyConflict);
        }

        [Fact]
        public void Translate_Should_Return_Original_For_Unknown_Exception()
        {
            var ex = new InvalidOperationException();
            _translator.Translate(ex).Should().BeSameAs(ex);
        }

        private static DbUpdateException CreateDbUpdateException(
            string sqlState,
            string constraintName
        )
        {
            var pg = new PostgresException(
                "failed",
                "ERROR",
                "ERROR",
                sqlState,
                null,
                null,
                0,
                0,
                null,
                null,
                "public",
                "products",
                null,
                null,
                constraintName,
                null,
                null,
                null
            );
            return new DbUpdateException(ErrorMessages.SaveChangesError, pg);
        }
    }
}
