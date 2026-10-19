using System;
using Mype.Domain.Common;

namespace Mype.Domain.ProductCategories
{
    public sealed class ProductCategory : Entity
    {
        private ProductCategory() { }

        private ProductCategory(
            Guid id,
            Guid businessId,
            string name,
            string normalizedName,
            string color,
            bool isDefault,
            Guid userId,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            BusinessId = businessId;
            Name = name;
            NormalizedName = normalizedName;
            Color = color;
            IsDefault = isDefault;
            IsActive = true;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string NormalizedName { get; private set; } = string.Empty;
        public string Color { get; private set; } = string.Empty;
        public bool IsDefault { get; private set; }
        public bool IsActive { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public Guid UpdatedByUserId { get; private set; }
        public DateTimeOffset? DeactivatedAt { get; private set; }
        public uint Version { get; private set; }

        public static ProductCategory Create(
            Guid businessId,
            string name,
            string normalizedName,
            string color,
            bool isDefault,
            Guid userId,
            DateTimeOffset utcNow
        ) =>
            new(
                Guid.NewGuid(),
                businessId,
                name.Trim(),
                normalizedName,
                color,
                isDefault,
                userId,
                utcNow
            );

        public void Update(
            string name,
            string normalizedName,
            string color,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            Name = name.Trim();
            NormalizedName = normalizedName;
            Color = color;
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        public void Deactivate(Guid userId, DateTimeOffset utcNow)
        {
            IsActive = false;
            DeactivatedAt = utcNow;
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        public void Reactivate(Guid userId, DateTimeOffset utcNow)
        {
            IsActive = true;
            DeactivatedAt = null;
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }
    }
}
