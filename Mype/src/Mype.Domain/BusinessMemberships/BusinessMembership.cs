using System;
using Mype.Domain.Common;

namespace Mype.Domain.BusinessMemberships
{
    public class BusinessMembership : Entity
    {
        private BusinessMembership() { }

        private BusinessMembership(
            Guid id,
            Guid businessId,
            Guid userId,
            Guid roleId,
            Guid createdByUserId,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            BusinessId = businessId;
            UserId = userId;
            RoleId = roleId;
            Status = BusinessMembershipStatus.Active;
            JoinedAt = utcNow;
            CreatedByUserId = createdByUserId;
            UpdatedByUserId = createdByUserId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }

        public Guid UserId { get; private set; }

        public Guid RoleId { get; private set; }

        public BusinessMembershipStatus Status { get; private set; }

        public DateTimeOffset JoinedAt { get; private set; }

        public DateTimeOffset? DeactivatedAt { get; private set; }

        public DateTimeOffset? ReactivatedAt { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public Guid UpdatedByUserId { get; private set; }

        public uint Version { get; private set; }

        public void Deactivate(Guid currentUserId, DateTimeOffset utcNow)
        {
            Status = BusinessMembershipStatus.Inactive;
            DeactivatedAt = utcNow;
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

        public void Reactivate(Guid currentUserId, DateTimeOffset utcNow)
        {
            Status = BusinessMembershipStatus.Active;
            ReactivatedAt = utcNow;
            DeactivatedAt = null;
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

        public static BusinessMembership CreateOwner(
            Guid businessId,
            Guid userId,
            Guid ownerRoleId,
            DateTimeOffset utcNow
        )
        {
            return new BusinessMembership(
                Guid.NewGuid(),
                businessId,
                userId,
                ownerRoleId,
                userId,
                utcNow
            );
        }

        public bool IsActive()
        {
            return Status == BusinessMembershipStatus.Active;
        }
    }
}
