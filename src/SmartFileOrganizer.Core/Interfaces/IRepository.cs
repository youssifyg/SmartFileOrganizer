using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IRepository
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task SaveFileRecordAsync(FileRecord record, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FileRecord>> GetAllFileRecordsAsync(CancellationToken cancellationToken = default);
        Task SaveDuplicateGroupAsync(DuplicateGroup group, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DuplicateGroup>> GetDuplicateGroupsAsync(CancellationToken cancellationToken = default);
        Task SaveScanSessionAsync(ScanSession session, CancellationToken cancellationToken = default);
        Task<ScanSession> GetLatestScanSessionAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScanSession>> GetScanSessionsAsync(CancellationToken cancellationToken = default);
        Task SaveOperationHistoryAsync(OperationHistoryEntry entry, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OperationHistoryEntry>> GetOperationHistoryAsync(CancellationToken cancellationToken = default);
        Task DeleteFileRecordAsync(string filePath, CancellationToken cancellationToken = default);
        Task ClearAllDataAsync();
        Task ClearDuplicatesAsync();
    }
}
