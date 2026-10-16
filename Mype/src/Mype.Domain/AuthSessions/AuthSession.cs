using System;
using Mype.Domain.Common;

namespace Mype.Domain.AuthSessions
{
    public class AuthSession : Entity
    {
        private AuthSession() { }

        private AuthSession(
            Guid id,
            Guid familyId,
            Guid userId,
            string refreshTokenVerifier,
            DateTimeOffset createdAt,
            DateTimeOffset idleExpiresAt,
            DateTimeOffset absoluteExpiresAt
        )
            : base(id)
        {
            FamilyId = familyId;
            UserId = userId;
            RefreshTokenVerifier = refreshTokenVerifier;
            CreatedAt = createdAt;
            UpdatedAt = createdAt;
            IdleExpiresAt = idleExpiresAt;
            AbsoluteExpiresAt = absoluteExpiresAt;
        }

        public Guid FamilyId { get; private set; }
        public Guid UserId { get; private set; }
        public string RefreshTokenVerifier { get; private set; } = string.Empty;
        public DateTimeOffset IdleExpiresAt { get; private set; }
        public DateTimeOffset AbsoluteExpiresAt { get; private set; }
        public DateTimeOffset? RevokedAt { get; private set; }
        public AuthSessionRevocationReason? RevocationReason { get; private set; }
        public Guid? ReplacedBySessionId { get; private set; }
        public uint Version { get; private set; }

        public static AuthSession Create(
            Guid userId,
            string refreshTokenVerifier,
            DateTimeOffset utcNow,
            DateTimeOffset idleExpiresAt,
            DateTimeOffset absoluteExpiresAt
        )
        {
            return new AuthSession(
                Guid.NewGuid(),
                Guid.NewGuid(),
                userId,
                refreshTokenVerifier,
                utcNow,
                idleExpiresAt,
                absoluteExpiresAt
            );
        }

        public AuthSession Rotate(
            string replacementVerifier,
            DateTimeOffset utcNow,
            DateTimeOffset replacementIdleExpiresAt
        )
        {
            EnsureActive(utcNow);

            var replacement = new AuthSession(
                Guid.NewGuid(),
                FamilyId,
                UserId,
                replacementVerifier,
                utcNow,
                replacementIdleExpiresAt < AbsoluteExpiresAt
                    ? replacementIdleExpiresAt
                    : AbsoluteExpiresAt,
                AbsoluteExpiresAt
            );

            Revoke(AuthSessionRevocationReason.Rotated, utcNow, replacement.Id);
            return replacement;
        }

        public void Revoke(
            AuthSessionRevocationReason reason,
            DateTimeOffset utcNow,
            Guid? replacedBySessionId = null
        )
        {
            if (RevokedAt.HasValue)
            {
                return;
            }

            RevokedAt = utcNow;
            RevocationReason = reason;
            ReplacedBySessionId = replacedBySessionId;
            UpdatedAt = utcNow;
        }

        public bool IsActive(DateTimeOffset utcNow)
        {
            return !RevokedAt.HasValue && utcNow < IdleExpiresAt && utcNow < AbsoluteExpiresAt;
        }

        public bool IsRotated()
        {
            return RevocationReason == AuthSessionRevocationReason.Rotated
                && ReplacedBySessionId.HasValue;
        }

        private void EnsureActive(DateTimeOffset utcNow)
        {
            if (!IsActive(utcNow))
            {
                throw new InvalidOperationException("La sesión no se encuentra activa.");
            }
        }
    }
}
