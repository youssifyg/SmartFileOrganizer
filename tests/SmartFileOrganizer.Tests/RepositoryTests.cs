using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class RepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly SQLiteRepository _repository;

        public RepositoryTests()
        {
            // Use a temporary file for SQLite database
            _dbPath = Path.GetTempFileName();
            _repository = new SQLiteRepository(_dbPath);
            _repository.InitializeAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            // Ensure SQLite releases any file handles before deleting the temporary DB files
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            }
            catch { }

            // Force garbage collection to finalize any pending disposals
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // If using in-memory database, no file to delete
            if (string.Equals(_dbPath, ":memory:", StringComparison.OrdinalIgnoreCase))
                return;

            // Delete the main DB file and any auxiliary files (journal, wal, etc.)
            var directory = Path.GetDirectoryName(_dbPath) ?? ".";
            var baseName = Path.GetFileName(_dbPath);
            var files = Directory.GetFiles(directory, baseName + "*");
            const int maxAttempts = 5;
            foreach (var file in files)
            {
                for (int i = 0; i < maxAttempts; i++)
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                        break; // success
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        [Fact]
        public async Task SaveAndRetrieveFileRecord()
        {
            var record = new FileRecord
            {
                Path = "C:\\temp\\file.txt",
                Size = 1234,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };
            await _repository.SaveFileRecordAsync(record);
            var all = await _repository.GetAllFileRecordsAsync();
            Assert.Single(all);
            var retrieved = all.First();
            Assert.Equal(record.Path, retrieved.Path);
            Assert.Equal(record.Size, retrieved.Size);
        }

        [Fact]
        public async Task SaveAndRetrieveHashCache()
        {
            var cache = new HashCache
            {
                Path = "C:\\temp\\file2.bin",
                Size = 4096,
                LastModified = DateTime.UtcNow,
                PartialHash = new byte[] { 1, 2, 3 },
                FullHash = new byte[] { 4, 5, 6 }
            };
            await _repository.SaveHashCacheAsync(cache);
            var retrieved = await _repository.GetHashCacheAsync(cache.Path, cache.Size, cache.LastModified);
            Assert.NotNull(retrieved);
            Assert.Equal(cache.Path, retrieved.Path);
            Assert.Equal(cache.Size, retrieved.Size);
            Assert.Equal(cache.PartialHash, retrieved.PartialHash);
            Assert.Equal(cache.FullHash, retrieved.FullHash);
        }

        [Fact]
        public async Task SaveAndRetrieveOperationHistory()
        {
            var entry = new OperationHistoryEntry
            {
                Action = "Delete",
                FilePath = "C:\\temp\\deleted.txt",
                Timestamp = DateTime.UtcNow,
                Size = 1024
            };
            await _repository.SaveOperationHistoryAsync(entry);
            var history = await _repository.GetOperationHistoryAsync();
            Assert.Single(history);
            var retrieved = history.First();
            Assert.Equal(entry.Action, retrieved.Action);
            Assert.Equal(entry.FilePath, retrieved.FilePath);
            Assert.Equal(entry.Size, retrieved.Size);
        }
    }
}
