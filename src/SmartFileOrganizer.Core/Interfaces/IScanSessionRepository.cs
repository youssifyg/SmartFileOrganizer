using SmartFileOrganizer.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IScanSessionRepository
    {
        Task SaveSessionAsync(ScanSession session);
        Task<ScanSession> GetSessionByIdAsync(int id);
        Task<IEnumerable<ScanSession>> GetAllSessionsAsync();
    }
}
