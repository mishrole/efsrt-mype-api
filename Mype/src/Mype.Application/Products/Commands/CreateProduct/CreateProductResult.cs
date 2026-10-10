using System;

namespace Mype.Application.Products.Commands.CreateProduct
{
    public sealed class CreateProductResult
    {
        public Guid Id { get; set; }

        public Guid BusinessId { get; set; }

        public Guid CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal SalePrice { get; set; }

        public decimal UnitCost { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
