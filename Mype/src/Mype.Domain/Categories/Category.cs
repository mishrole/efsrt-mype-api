using Mype.Domain.Common;
using System;

namespace Mype.Domain.Categories
{
    public class Category : Entity
    {
        private Category()
        {
        }

        private Category(
            Guid id,
            Guid businessId,
            CategoryType type,
            string name,
            string normalizedName,
            bool isDefault,
            Guid userId,
            DateTimeOffset utcNow
        ) : base(id)
        {
            BusinessId = businessId;
            Type = type;
            Name = name;
            NormalizedName = normalizedName;
            IsDefault = isDefault;
            IsActive = true;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }

        public CategoryType Type { get; private set; }

        public string Name { get; private set; } =
            string.Empty;

        public string NormalizedName { get; private set; } =
            string.Empty;

        public bool IsDefault { get; private set; }

        public bool IsActive { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public Guid UpdatedByUserId { get; private set; }

        public DateTimeOffset? DeactivatedAt { get; private set; }

        public uint Version { get; private set; }

        public void Deactivate(
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            IsActive = false;
            DeactivatedAt = utcNow;
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

        public static Category CreateDefault(
            Guid businessId,
            CategoryType type,
            string name,
            string normalizedName,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            return new Category(
                Guid.NewGuid(),
                businessId,
                type,
                name.Trim(),
                normalizedName,
                true,
                userId,
                utcNow
            );
        }
    }
}