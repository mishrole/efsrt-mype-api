using System;

namespace Mype.Infrastructure.Persistence.Exceptions
{
    public interface IPersistenceExceptionTranslator
    {
        Exception Translate(Exception exception);
    }
}
