using System;
using Mype.Domain.Common;

namespace Mype.Domain.Users
{
    public class User : Entity
    {
        private User() { }

        private User(
            Guid id,
            string email,
            string normalizedEmail,
            string passwordHash,
            string displayName,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            Email = email;
            NormalizedEmail = normalizedEmail;
            PasswordHash = passwordHash;
            DisplayName = displayName;
            EmailVerified = false;
            Status = UserStatus.Active;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public string Email { get; private set; } = string.Empty;

        public string NormalizedEmail { get; private set; } = string.Empty;

        public string PasswordHash { get; private set; } = string.Empty;

        public string DisplayName { get; private set; } = string.Empty;

        public bool EmailVerified { get; private set; }

        public UserStatus Status { get; private set; }

        public DateTimeOffset? DeactivatedAt { get; private set; }

        public uint Version { get; private set; }

        public static User Create(
            string email,
            string normalizedEmail,
            string passwordHash,
            string displayName,
            DateTimeOffset utcNow
        )
        {
            return new User(
                Guid.NewGuid(),
                email,
                normalizedEmail,
                passwordHash,
                displayName,
                utcNow
            );
        }

        public void Deactivate(DateTimeOffset utcNow)
        {
            Status = UserStatus.Inactive;
            DeactivatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public void Reactivate(DateTimeOffset utcNow)
        {
            Status = UserStatus.Active;
            DeactivatedAt = null;
            UpdatedAt = utcNow;
        }

        public bool IsActive()
        {
            return Status == UserStatus.Active;
        }
    }
}
