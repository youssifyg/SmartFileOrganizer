using System.Threading;
using System.Threading.Tasks;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface ISimilarityEngine
    {
        bool SupportsFileType( string extension );
        Task<float> CalculateSimilarityAsync( string sourcePath, string targetPath, CancellationToken cancellationToken = default );
    }
}
