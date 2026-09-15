using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure.Data;

namespace SmartFileOrganizer.Infrastructure.Data.Repositories
{
    public class OperationHistoryRepository : IOperationHistoryRepository
    {
        private readonly AppDbContext _context;

        public OperationHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddEntryAsync(OperationHistoryEntry entry)
        {
            await _context.OperationHistory.AddAsync(entry);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OperationHistoryEntry>> GetAllEntriesAsync()
        {
            return await _context.OperationHistory.AsNoTracking().ToListAsync();
        }
    }
}
