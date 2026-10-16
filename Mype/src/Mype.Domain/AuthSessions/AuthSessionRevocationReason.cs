namespace Mype.Domain.AuthSessions
{
    public enum AuthSessionRevocationReason
    {
        Rotated = 1,
        Logout = 2,
        ReuseDetected = 3,
        PasswordChanged = 4,
        AccountDeactivated = 5,
        IdleExpired = 6,
        AbsoluteExpired = 7,
    }
}
