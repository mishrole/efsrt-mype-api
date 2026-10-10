using System;
using Mype.Domain.Users;

namespace Mype.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserResult
    {
        public Guid UserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UserStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
