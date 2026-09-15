using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class PlaceholderSimilarityEngine : ISimilarityEngine
    {
        public bool SupportsFileType( string extension )
        {
            return false;
        }

        public Task<float> CalculateSimilarityAsync( string sourcePath, string targetPath, CancellationToken cancellationToken = default )
        {
            return Task.FromResult( 0.0f );
        }
    }
}
