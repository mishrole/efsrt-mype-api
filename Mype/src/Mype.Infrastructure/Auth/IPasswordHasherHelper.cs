namespace Mype.Infrastructure.Auth
{
    public interface IPasswordHasherHelper
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}
