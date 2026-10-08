using System;

namespace Mype.Api.Context
{
    public interface IUserContextProvider
    {
        Guid GetCurrentUserId();
        bool IsAuthenticated { get; }
    }
}
