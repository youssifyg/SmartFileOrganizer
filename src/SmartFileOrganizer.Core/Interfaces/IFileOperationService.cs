using System.Threading;
using System.Threading.Tasks;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IFileOperationService
    {
        Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
        Task<bool> MoveToRecycleBinAsync(string filePath);
    }
}

