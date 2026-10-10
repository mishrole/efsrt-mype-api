using System;
using Mype.Domain.Categories;

namespace Mype.Application.Businesses.Commands.CreateBusiness
{
    public class CategoryResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public CategoryType Type { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }
    }
}
