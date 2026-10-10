using Mype.Domain.Common;
using System;

namespace Mype.Domain.Products
{
    public class Product : Entity
    {
        private Product()
        {
        }

        private Product(
            Guid id,
            Guid businessId,
            Guid categoryId,
            string name,
            string normalizedName,
            decimal salePrice,
            decimal unitCost,
            Guid userId,
            DateTimeOffset utcNow
        ) : base(id)
        {
            BusinessId = businessId;
            CategoryId = categoryId;
            Name = name;
            NormalizedName = normalizedName;
            SalePrice = salePrice;
            UnitCost = unitCost;
            IsActive = true;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }

        public Guid CategoryId { get; private set; }

        public string Name { get; private set; } =
            string.Empty;

        public string NormalizedName { get; private set; } =
            string.Empty;

        public decimal SalePrice { get; private set; }

        public decimal UnitCost { get; private set; }

        public bool IsActive { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public Guid UpdatedByUserId { get; private set; }

        public DateTimeOffset? DeactivatedAt
        {
            get;
            private set;
        }

        public uint Version { get; private set; }

        public bool IsAvailable()
        {
            return IsActive;
        }

        public void Update(
            Guid categoryId,
            string name,
            string normalizedName,
            decimal salePrice,
            decimal unitCost,
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            CategoryId = categoryId;
            Name = name;
            NormalizedName = normalizedName;
            SalePrice = salePrice;
            UnitCost = unitCost;
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

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

        public void Reactivate(
            Guid currentUserId,
            DateTimeOffset utcNow
        )
        {
            IsActive = true;
            DeactivatedAt = null;
            UpdatedByUserId = currentUserId;
            UpdatedAt = utcNow;
        }

        public static Product Create(
            Guid businessId,
            Guid categoryId,
            string name,
            string normalizedName,
            decimal salePrice,
            decimal unitCost,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            return new Product(
                Guid.NewGuid(),
                businessId,
                categoryId,
                name,
                normalizedName,
                salePrice,
                unitCost,
                userId,
                utcNow
            );
        }
    }
}
