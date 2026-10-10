using System.Collections.Generic;
using Mype.Application.Categories.Interfaces;
using Mype.Application.Categories.Models;
using Mype.Domain.Categories;

namespace Mype.Infrastructure.Categories
{
    public class DefaultCategoryProvider : IDefaultCategoryProvider
    {
        private static readonly IReadOnlyCollection<DefaultCategoryDefinition> DefaultCategories =
        [
            new("Productos", CategoryType.Sale),
            new("Servicios", CategoryType.Sale),
            new("Otros ingresos", CategoryType.Sale),
            new("Mercadería e insumos", CategoryType.Expense),
            new("Servicios", CategoryType.Expense),
            new("Transporte", CategoryType.Expense),
            new("Alquiler", CategoryType.Expense),
            new("Publicidad", CategoryType.Expense),
            new("Impuestos", CategoryType.Expense),
            new("Otros gastos", CategoryType.Expense),
        ];

        public IReadOnlyCollection<DefaultCategoryDefinition> GetDefaultCategories()
        {
            return DefaultCategories;
        }
    }
}
