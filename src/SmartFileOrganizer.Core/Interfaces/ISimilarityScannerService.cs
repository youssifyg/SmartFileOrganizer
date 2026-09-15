using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface ISimilarityScannerService
    {
        Task<IReadOnlyList<SimilarityGroup>> DetectSimilarFilesAsync( IEnumerable<FileRecord> records, CancellationToken cancellationToken = default );
    }
}
