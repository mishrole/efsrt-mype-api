using System.Collections.Generic;
using Mype.Application.Categories.Models;

namespace Mype.Application.Categories.Interfaces
{
    public interface IDefaultCategoryProvider
    {
        IReadOnlyCollection<DefaultCategoryDefinition> GetDefaultCategories();
    }
}
