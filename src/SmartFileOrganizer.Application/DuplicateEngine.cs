using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Application
{
    public class DuplicateEngine
    {
        private readonly IHashService _hashService;

        public DuplicateEngine(IHashService hashService)
        {
            _hashService = hashService;
        }

        public async Task<IReadOnlyList<DuplicateGroup>> DetectDuplicatesAsync(IEnumerable<FileRecord> records, CancellationToken cancellationToken = default)
        {
            // Level 1: group by size
            var sizeGroups = records
                .GroupBy(r => r.Size)
                .Where(g => g.Count() > 1)
                .ToList();

            var candidateRecords = new List<FileRecord>();
            foreach (var g in sizeGroups)
                candidateRecords.AddRange(g);

            // Level 2: partial hash
            var partialHashDict = new Dictionary<string, List<FileRecord>>(); // key: hex string of partial hash
            foreach (var rec in candidateRecords)
            {
                var partialHash = await _hashService.CalculatePartialHashAsync(rec.Path, cancellationToken: cancellationToken);
                if (partialHash == null || partialHash.Length == 0)
                {
                    continue;
                }
                var key = System.Convert.ToHexString(partialHash);
                if (!partialHashDict.ContainsKey(key))
                    partialHashDict[key] = new List<FileRecord>();
                partialHashDict[key].Add(rec);
            }
            var afterPartial = partialHashDict.Values.Where(v => v.Count > 1).SelectMany(v => v).ToList();

            // Level 3: full hash
            var fullHashDict = new Dictionary<string, List<FileRecord>>();
            foreach (var rec in afterPartial)
            {
                var fullHash = await _hashService.CalculateFullHashAsync(rec.Path, cancellationToken);
                var key = System.Convert.ToHexString(fullHash);
                if (!fullHashDict.ContainsKey(key))
                    fullHashDict[key] = new List<FileRecord>();
                fullHashDict[key].Add(rec);
            }

            // Build duplicate groups
            var groups = new List<DuplicateGroup>();
            foreach (var group in fullHashDict.Values.Where(v => v.Count > 1))
            {
                var totalSize = group.Sum(r => r.Size);
                var recoverable = totalSize - group.First().Size; // keep one file
                groups.Add(new DuplicateGroup
                {
                    Files = group,
                    TotalSize = totalSize,
                    RecoverableSize = recoverable
                });
            }

            return groups;
        }
    }
}
