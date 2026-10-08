using Mype.Domain.Categories;
using System.Threading;
using System.Threading.Tasks;

namespace Mype.Application.Categories.Interfaces
{
    public interface ICategoryRepository
    {
        Task AddAsync(
            Category category,
            CancellationToken cancellationToken
        );
    }
}