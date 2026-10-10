using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Mype.Application.Common.Exceptions;
using Mype.Infrastructure.Persistence.Constraints;
using Mype.Shared.Constants;
using Npgsql;

namespace Mype.Infrastructure.Persistence.Exceptions
{
    public class PostgreSqlExceptionTranslator : IPersistenceExceptionTranslator
    {
        private static readonly Dictionary<
            string,
            Func<Exception, Exception>
        > UniqueConstraintMappings = new()
        {
            {
                DatabaseConstraints.Users.NormalizedEmail,
                exception => new ApplicationErrorException(
                    ErrorCodes.EmailAlreadyRegistered,
                    ErrorMessages.EmailAlreadyRegistered,
                    ApplicationErrorType.Conflict,
                    exception
                )
            },
            {
                DatabaseConstraints.Products.BusinessName,
                exception => new ApplicationErrorException(
                    ErrorCodes.ProductAlreadyExists,
                    ErrorMessages.ProductAlreadyExists,
                    ApplicationErrorType.Conflict,
                    exception
                )
            },
        };

        public Exception Translate(Exception exception)
        {
            if (exception is DbUpdateConcurrencyException)
            {
                return new ApplicationErrorException(
                    ErrorCodes.ConcurrencyConflict,
                    ErrorMessages.ConcurrencyConflict,
                    ApplicationErrorType.Conflict,
                    exception
                );
            }

            if (
                exception is DbUpdateException
                {
                    InnerException: PostgresException postgresException
                }
            )
            {
                return TranslatePostgresException(postgresException, exception);
            }

            return exception;
        }

        private static Exception TranslatePostgresException(
            PostgresException postgresException,
            Exception originalException
        )
        {
            if (
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName != null
                && UniqueConstraintMappings.TryGetValue(
                    postgresException.ConstraintName,
                    out var exceptionFactory
                )
            )
            {
                return exceptionFactory(originalException);
            }

            return originalException;
        }
    }
}
