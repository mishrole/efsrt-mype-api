using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mype.Application.Common.Exceptions;
using Mype.Infrastructure.Persistence.Constraints;
using Mype.Infrastructure.Persistence.Exceptions;
using Mype.Shared.Constants;
using Npgsql;

namespace Mype.Tests.Infrastructure.Persistence.Exceptions
{
    public sealed class PostgreSqlExceptionTranslatorTests
    {
        private readonly PostgreSqlExceptionTranslator _translator = new();

        [Theory]
        [InlineData(DatabaseConstraints.Users.NormalizedEmail, ErrorCodes.EmailAlreadyRegistered)]
        [InlineData(DatabaseConstraints.Products.BusinessName, ErrorCodes.ProductAlreadyExists)]
        [InlineData(
            DatabaseConstraints.ProductCategories.BusinessNormalizedName,
            ErrorCodes.ProductCategoryAlreadyExists
        )]
        public void Translate_Should_Map_Known_Unique_Constraints(
            string constraintName,
            string expectedErrorCode
        )
        {
            var translatedException = _translator.Translate(
                CreateDbUpdateException(PostgresErrorCodes.UniqueViolation, constraintName)
            );

            translatedException
                .Should()
                .BeOfType<ApplicationErrorException>()
                .Which.Code.Should()
                .Be(expectedErrorCode);
        }

        [Fact]
        public void Translate_Should_Map_Concurrency_Conflict()
        {
            var persistenceException = new DbUpdateConcurrencyException("conflict");

            var translatedException = _translator.Translate(persistenceException);

            translatedException
                .Should()
                .BeOfType<ApplicationErrorException>()
                .Which.Code.Should()
                .Be(ErrorCodes.ConcurrencyConflict);
        }

        [Fact]
        public void Translate_Should_Return_Original_For_Unknown_Exception()
        {
            var originalException = new InvalidOperationException();

            _translator.Translate(originalException).Should().BeSameAs(originalException);
        }

        private static DbUpdateException CreateDbUpdateException(
            string sqlState,
            string constraintName
        )
        {
            var postgresException = new PostgresException(
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
                "product_categories",
                null,
                null,
                constraintName,
                null,
                null,
                null
            );
            return new DbUpdateException(ErrorMessages.SaveChangesError, postgresException);
        }
    }
}
