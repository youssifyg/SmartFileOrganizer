using System;
using System.Collections.Concurrent;
using System.IO;

namespace SmartFileOrganizer.Core.Models
{
    /// <summary>
    /// In-memory hash cache that validates entries against file LastWriteTimeUtc.
    /// Thread-safe for parallel hashing operations.
    /// </summary>
    public class HashCacheService
    {
        private readonly ConcurrentDictionary<string, (string Hash, DateTime ModTime)> _cache = new();

        public bool TryGetHash(string filePath, out string hash)
        {
            if (_cache.TryGetValue(filePath, out var cached))
            {
                try
                {
                    var lastMod = new FileInfo(filePath).LastWriteTimeUtc;
                    if (lastMod == cached.ModTime)
                    {
                        hash = cached.Hash;
                        return true;
                    }
                }
                catch (Exception) { /* Fallback to false if file is inaccessible */ }
            }
            hash = null!;
            return false;
        }

        public void SetHash(string filePath, string hash)
        {
            try
            {
                var lastMod = new FileInfo(filePath).LastWriteTimeUtc;
                _cache[filePath] = (hash, lastMod);
            }
            catch (Exception) { /* Ignore if file deleted/locked */ }
        }
    }
}
