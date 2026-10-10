using System;
using Mype.Domain.Common;
using Mype.Domain.Products;

namespace Mype.Domain.FinancialMovements
{
    public class FinancialMovementItem : Entity
    {
        private FinancialMovementItem() { }

        private FinancialMovementItem(
            Guid id,
            Guid businessId,
            Guid movementId,
            Product product,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            BusinessId = businessId;
            MovementId = movementId;
            ApplyProduct(product);
            ApplyAmounts(quantity, unitAmount);
            IsActive = true;
            CreatedByUserId = userId;
            UpdatedByUserId = userId;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        public Guid BusinessId { get; private set; }
        public Guid MovementId { get; private set; }
        public Guid CategoryId { get; private set; }
        public Guid? ProductId { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public decimal Quantity { get; private set; }
        public decimal UnitAmount { get; private set; }
        public decimal? UnitCostSnapshot { get; private set; }
        public decimal SubtotalAmount { get; private set; }
        public bool IsActive { get; private set; }
        public Guid CreatedByUserId { get; private set; }
        public Guid UpdatedByUserId { get; private set; }
        public DateTimeOffset? RetiredAt { get; private set; }
        public uint Version { get; private set; }
        public decimal EstimatedCost =>
            RoundAmount(Quantity * UnitCostSnapshot.GetValueOrDefault());
        public decimal EstimatedMargin => SubtotalAmount - EstimatedCost;

        internal static FinancialMovementItem CreateSale(
            Guid businessId,
            Guid movementId,
            Product product,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureProductBusiness(product, businessId);
            return new(
                Guid.NewGuid(),
                businessId,
                movementId,
                product,
                quantity,
                unitAmount,
                userId,
                utcNow
            );
        }

        internal void UpdateSale(
            Product product,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            if (!IsActive)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.ItemAlreadyRetired
                );
            EnsureProductBusiness(product, BusinessId);
            ApplyProduct(product);
            ApplyAmounts(quantity, unitAmount);
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        internal void Retire(Guid userId, DateTimeOffset utcNow)
        {
            if (!IsActive)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.ItemAlreadyRetired
                );
            IsActive = false;
            RetiredAt = utcNow;
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        private void ApplyProduct(Product product)
        {
            ProductId = product.Id;
            CategoryId = product.CategoryId;
            Description = product.Name;
            UnitCostSnapshot = product.UnitCost;
        }

        private void ApplyAmounts(decimal quantity, decimal unitAmount)
        {
            if (quantity <= 0m)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.InvalidQuantity
                );
            if (unitAmount < 0m)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.InvalidUnitAmount
                );
            Quantity = quantity;
            UnitAmount = unitAmount;
            SubtotalAmount = RoundAmount(quantity * unitAmount);
        }

        private static void EnsureProductBusiness(Product product, Guid businessId)
        {
            if (product == null || product.BusinessId != businessId)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.ProductBusinessMismatch
                );
        }

        private static decimal RoundAmount(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
