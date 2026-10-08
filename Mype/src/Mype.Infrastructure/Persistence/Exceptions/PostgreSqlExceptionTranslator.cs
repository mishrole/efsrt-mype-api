using Microsoft.EntityFrameworkCore;
using Mype.Application.Common.Exceptions;
using Mype.Shared.Constants;
using Npgsql;
using System;
using System.Collections.Generic;

namespace Mype.Infrastructure.Persistence.Exceptions
{
    public class PostgreSqlExceptionTranslator
        : IPersistenceExceptionTranslator
    {
        private static readonly Dictionary<string, Func<Exception, Exception>>
            UniqueConstraintMappings = new()
            {
                {
                    DatabaseConstraints.UsersNormalizedEmail,
                    exception => new ApplicationErrorException(
                        ErrorCodes.EmailAlreadyRegistered,
                        ErrorMessages.EmailAlreadyRegistered,
                        ApplicationErrorType.Conflict,
                        exception
                    )
                }
            };

        public Exception Translate(Exception exception)
        {
            if (
                exception is DbUpdateException
                {
                    InnerException: PostgresException postgresException
                }
            )
            {
                return TranslatePostgresException(
                    postgresException,
                    exception
                );
            }

            return exception;
        }

        private static Exception TranslatePostgresException(
            PostgresException postgresException,
            Exception originalException
        )
        {
            if (
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName != null &&
                UniqueConstraintMappings.TryGetValue(
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
