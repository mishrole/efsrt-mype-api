using System;
using Mype.Domain.Common;
using Mype.Domain.FinancialMovements.Constraints;
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
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
            : base(id)
        {
            BusinessId = businessId;
            MovementId = movementId;
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
            var item = new FinancialMovementItem(
                Guid.NewGuid(),
                businessId,
                movementId,
                quantity,
                unitAmount,
                userId,
                utcNow
            );
            item.ApplyProduct(product);
            return item;
        }

        internal static FinancialMovementItem CreateExpense(
            Guid businessId,
            Guid movementId,
            Guid categoryId,
            string description,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            var item = new FinancialMovementItem(
                Guid.NewGuid(),
                businessId,
                movementId,
                quantity,
                unitAmount,
                userId,
                utcNow
            );
            item.ApplyExpense(categoryId, description);
            return item;
        }

        internal void UpdateSale(
            Product product,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureActive();
            EnsureProductBusiness(product, BusinessId);
            ApplyProduct(product);
            ApplyAmounts(quantity, unitAmount);
            Touch(userId, utcNow);
        }

        internal void UpdateExpense(
            Guid categoryId,
            string description,
            decimal quantity,
            decimal unitAmount,
            Guid userId,
            DateTimeOffset utcNow
        )
        {
            EnsureActive();
            var normalizedDescription = ValidateExpense(categoryId, description);
            ValidateAmounts(quantity, unitAmount);
            CategoryId = categoryId;
            Description = normalizedDescription;
            ProductId = null;
            UnitCostSnapshot = null;
            ApplyAmounts(quantity, unitAmount);
            Touch(userId, utcNow);
        }

        internal void Retire(Guid userId, DateTimeOffset utcNow)
        {
            EnsureActive();
            IsActive = false;
            RetiredAt = utcNow;
            Touch(userId, utcNow);
        }

        private void ApplyProduct(Product product)
        {
            ProductId = product.Id;
            CategoryId = product.CategoryId;
            Description = product.Name;
            UnitCostSnapshot = product.UnitCost;
        }

        private void ApplyExpense(Guid categoryId, string description)
        {
            var normalizedDescription = ValidateExpense(categoryId, description);
            CategoryId = categoryId;
            Description = normalizedDescription;
            ProductId = null;
            UnitCostSnapshot = null;
        }

        private static string ValidateExpense(Guid categoryId, string description)
        {
            if (categoryId == Guid.Empty)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.InvalidCategory
                );
            var normalizedDescription = description?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedDescription))
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.InvalidDescription
                );
            if (
                normalizedDescription.Length > FinancialMovementItemConstraints.DescriptionMaxLength
            )
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.DescriptionTooLong
                );
            return normalizedDescription;
        }

        private static void ValidateAmounts(decimal quantity, decimal unitAmount)
        {
            if (quantity <= 0m)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.InvalidQuantity
                );
            if (unitAmount < 0m)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.InvalidUnitAmount
                );
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

        private void EnsureActive()
        {
            if (!IsActive)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.ItemAlreadyRetired
                );
        }

        private static void EnsureProductBusiness(Product product, Guid businessId)
        {
            if (product == null || product.BusinessId != businessId)
                throw new FinancialMovementItemException(
                    FinancialMovementItemError.ProductBusinessMismatch
                );
        }

        private void Touch(Guid userId, DateTimeOffset utcNow)
        {
            UpdatedByUserId = userId;
            UpdatedAt = utcNow;
        }

        private static decimal RoundAmount(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
