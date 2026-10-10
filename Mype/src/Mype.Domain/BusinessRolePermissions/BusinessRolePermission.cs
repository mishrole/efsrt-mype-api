using System;

namespace Mype.Domain.BusinessRolePermissions
{
    public class BusinessRolePermission
    {
        private BusinessRolePermission() { }

        private BusinessRolePermission(
            Guid businessRoleId,
            Guid permissionId,
            DateTimeOffset createdAt
        )
        {
            BusinessRoleId = businessRoleId;
            PermissionId = permissionId;
            CreatedAt = createdAt;
        }

        public Guid BusinessRoleId { get; private set; }

        public Guid PermissionId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public static BusinessRolePermission Create(
            Guid businessRoleId,
            Guid permissionId,
            DateTimeOffset utcNow
        )
        {
            return new BusinessRolePermission(businessRoleId, permissionId, utcNow);
        }
    }
}
