namespace Mype.Api.Context
{
    public interface IUserContextProvider
    {
        string GetCurrentUserId();
        bool IsAuthenticated { get; }
    }
}
