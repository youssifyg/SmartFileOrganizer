// IBatchOperationService.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Interfaces
{
    /// <summary>
    /// Service for executing batch file operations (e.g., safe delete) on a collection of FileRecord items.
    /// </summary>
    public interface IBatchOperationService
    {
        /// <summary>
        /// Executes the batch operation asynchronously.
        /// </summary>
        /// <param name="files">The files to operate on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ExecuteAsync(IEnumerable<FileRecord> files, CancellationToken cancellationToken);
    }
}
