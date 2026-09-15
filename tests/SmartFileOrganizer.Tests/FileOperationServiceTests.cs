using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartFileOrganizer.Infrastructure;
using SmartFileOrganizer.Core.Models;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class FileOperationServiceTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly SQLiteRepository _repository;
        private readonly FileOperationService _service;

        public FileOperationServiceTests()
        {
            // Use a temporary file for SQLite database
            _dbPath = Path.GetTempFileName();
            _repository = new SQLiteRepository(_dbPath);
            _repository.InitializeAsync().GetAwaiter().GetResult();
            _service = new FileOperationService(_repository);
        }

        public void Dispose()
        {
            // Ensure SQLite releases any file handles before deleting the temporary DB file
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

            // Retry deletion in case the file is still locked
            const int maxAttempts = 5;
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    if (File.Exists(_dbPath))
                        File.Delete(_dbPath);
                    break; // success
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        [Fact]
        public async Task DeleteAsync_MovesFileToRecycleBin_AndLogsHistory()
        {
            // Create a temporary file
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");
            var fileInfo = new FileInfo(tempFile);
            var size = fileInfo.Length;

            // Perform safe delete
            await _service.DeleteAsync(tempFile);

            // Verify file no longer exists at original location
            Assert.False(File.Exists(tempFile), "File should be removed after DeleteAsync");

            // Verify operation history logged
            var history = await _repository.GetOperationHistoryAsync();
            Assert.Single(history);
            var entry = history.First();
            Assert.Equal("Delete", entry.Action);
            Assert.Equal(tempFile, entry.FilePath);
            Assert.Equal(size, entry.Size);
        }
    }
}
