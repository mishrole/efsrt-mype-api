namespace Mype.Application.Auth.Commands.Logout
{
    public sealed class LogoutResult
    {
        public static readonly LogoutResult Completed = new();

        private LogoutResult() { }
    }
}
