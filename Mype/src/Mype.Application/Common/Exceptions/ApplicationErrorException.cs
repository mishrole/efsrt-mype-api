using System;

namespace Mype.Application.Common.Exceptions
{
    public class ApplicationErrorException : Exception
    {
        public ApplicationErrorException(
            string code,
            string message,
            ApplicationErrorType errorType,
            Exception innerException = null
        )
            : base(message, innerException)
        {
            Code = code;
            ErrorType = errorType;
        }

        public string Code { get; }

        public ApplicationErrorType ErrorType { get; }
    }
}
