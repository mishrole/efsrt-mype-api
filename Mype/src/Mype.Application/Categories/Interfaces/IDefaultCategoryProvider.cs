using Mype.Application.Categories.Models;
using System.Collections.Generic;

namespace Mype.Application.Categories.Interfaces
{
    public interface IDefaultCategoryProvider
    {
        IReadOnlyCollection<DefaultCategoryDefinition> GetDefaultCategories();
    }
}