using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure.Data;

namespace SmartFileOrganizer.Infrastructure.Data.Repositories
{
    public class ScanSessionRepository : IScanSessionRepository
    {
        private readonly AppDbContext _context;

        public ScanSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveSessionAsync(ScanSession session)
        {
            await _context.ScanSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task<ScanSession> GetSessionByIdAsync(int id)
        {
            return await _context.ScanSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<ScanSession>> GetAllSessionsAsync()
        {
            return await _context.ScanSessions.AsNoTracking().ToListAsync();
        }
    }
}
