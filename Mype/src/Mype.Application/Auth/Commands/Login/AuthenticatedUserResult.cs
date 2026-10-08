using Mype.Domain.Users;
using System;

namespace Mype.Application.Auth.Commands.Login
{
    public class AuthenticatedUserResult
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public UserStatus Status { get; set; }
    }
}