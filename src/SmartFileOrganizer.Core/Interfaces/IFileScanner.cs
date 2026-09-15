using System.Collections.Generic;
using System.Threading;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IFileScanner
    {
        // Existing method (if any)
        IAsyncEnumerable<string> ScanAsync(string rootPath, CancellationToken cancellationToken = default);
        // New method to enumerate files with detailed records
        IAsyncEnumerable<FileRecord> EnumerateFilesAsync(string path, CancellationToken cancellationToken = default);
    }
}
