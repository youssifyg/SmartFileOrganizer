using System;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface ITempCleanupService
    {
        /// <summary>
        /// Cleans user and system temporary folders.
        /// Returns a summary of the cleanup operation.
        /// </summary>
        Task<TempCleanupSummary> CleanAsync(string? customTempPath = null, CancellationToken cancellationToken = default);
    }
}
