using SmartFileOrganizer.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IOperationHistoryRepository
    {
        Task AddEntryAsync(OperationHistoryEntry entry);
        Task<IEnumerable<OperationHistoryEntry>> GetAllEntriesAsync();
    }
}
