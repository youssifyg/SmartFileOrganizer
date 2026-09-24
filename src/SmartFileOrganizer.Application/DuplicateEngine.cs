using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Application
{
    public class DuplicateEngine
    {
        private readonly IHashService _hashService;
        private readonly ILogger<DuplicateEngine> _logger;
        private readonly HashCacheService _hashCache;

        public DuplicateEngine(IHashService hashService, ILogger<DuplicateEngine> logger, HashCacheService hashCache)
        {
            _hashService = hashService;
            _logger = logger;
            _hashCache = hashCache;
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

            // Level 2: partial hash (parallel - 4 threads)
            var partialHashDict = new ConcurrentDictionary<string, ConcurrentBag<FileRecord>>();
            await Parallel.ForEachAsync(
                candidateRecords,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount),
                    CancellationToken = cancellationToken
                },
                async (rec, ct) =>
                {
                    try
                    {
                        var partialHash = await _hashService.CalculatePartialHashAsync(rec.Path, cancellationToken: ct);
                        if (partialHash == null || partialHash.Length == 0)
                            return;
                        var key = Convert.ToHexString(partialHash);
                        partialHashDict.GetOrAdd(key, _ => new ConcurrentBag<FileRecord>()).Add(rec);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning("File inaccessible during partial hash: {Path} - {Message}", rec.Path, ex.Message);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning("File access forbidden during partial hash: {Path} - {Message}", rec.Path, ex.Message);
                    }
                }
            );
            var afterPartial = partialHashDict.Values.Where(v => v.Count > 1).SelectMany(v => v).ToList();

            // Level 3: full hash (parallel - 2 threads, IO heavy)
            var fullHashDict = new ConcurrentDictionary<string, ConcurrentBag<FileRecord>>();
            await Parallel.ForEachAsync(
                afterPartial,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 2,
                    CancellationToken = cancellationToken
                },
                async (rec, ct) =>
                {
                    try
                    {
                        string? key;
                        if (_hashCache.TryGetHash(rec.Path!, out var cachedHash))
                        {
                            key = cachedHash;
                        }
                        else
                        {
                            var fullHash = await _hashService.CalculateFullHashAsync(rec.Path, ct);
                            if (fullHash == null || fullHash.Length == 0)
                                return;
                            key = Convert.ToHexString(fullHash);
                            _hashCache.SetHash(rec.Path!, key);
                        }
                        fullHashDict.GetOrAdd(key, _ => new ConcurrentBag<FileRecord>()).Add(rec);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning("File inaccessible during full hash: {Path} - {Message}", rec.Path, ex.Message);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogWarning("File access forbidden during full hash: {Path} - {Message}", rec.Path, ex.Message);
                    }
                }
            );

            // Build duplicate groups
            var groups = new List<DuplicateGroup>();
            foreach (var bag in fullHashDict.Values.Where(v => v.Count > 1))
            {
                var group = bag.ToList();
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
