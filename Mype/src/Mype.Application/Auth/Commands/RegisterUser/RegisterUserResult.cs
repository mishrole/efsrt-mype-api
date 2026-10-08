using Mype.Domain.Users;
using System;

namespace Mype.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserResult
    {
        public Guid UserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
