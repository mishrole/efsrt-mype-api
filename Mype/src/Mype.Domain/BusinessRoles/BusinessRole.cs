using Mype.Domain.Common;
using System;

namespace Mype.Domain.BusinessRoles
{
    public class BusinessRole : ReferenceEntity
    {
        private BusinessRole()
        {
        }

        private BusinessRole(
            Guid id,
            string code,
            string name,
            string description,
            bool isSystem,
            bool isActive,
            DateTimeOffset createdAt,
            DateTimeOffset updatedAt
        ) : base(
            id,
            code,
            name,
            isActive
        )
        {
            Description = description;
            IsSystem = isSystem;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public string Description { get; private set; } = string.Empty;

        public bool IsSystem { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset UpdatedAt { get; private set; }

        public static BusinessRole CreateSystem(
            Guid id,
            string code,
            string name,
            string description,
            DateTimeOffset utcNow
        )
        {
            return new BusinessRole(
                id,
                code,
                name,
                description,
                true,
                true,
                utcNow,
                utcNow
            );
        }
    }
}