using System.Threading;
using System.Threading.Tasks;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IHashService
    {
        Task<byte[]> CalculatePartialHashAsync(string filePath, int chunkSize = 4096, CancellationToken cancellationToken = default);
        Task<byte[]> CalculateFullHashAsync(string filePath, CancellationToken cancellationToken = default);
    }
}

