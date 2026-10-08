using Mype.Application.Common.Exceptions;

namespace Mype.Application.Common
{
    public sealed record ApplicationError(
        string Code,
        string Message,
        ApplicationErrorType Type
    );
}
