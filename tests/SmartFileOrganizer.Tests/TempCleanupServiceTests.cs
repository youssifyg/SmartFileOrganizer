using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure.Services;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class TempCleanupServiceTests
    {
        private string CreateTempFile(out long size)
        {
            var tempDir = Path.GetTempPath();
            var filePath = Path.Combine(tempDir, Guid.NewGuid().ToString() + ".txt");
            var content = new string('a', 1024); // 1 KB
            File.WriteAllText(filePath, content);
            size = new FileInfo(filePath).Length;
            return filePath;
        }

        [Fact]
        public async Task CleanAsync_DeletesUnlockedFilesAndReportsAccurately()
        {
            // Arrange: create isolated temp directory and file
            var isolatedTemp = Path.Combine(Path.GetTempPath(), "TempCleanupTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(isolatedTemp);
            var filePath = Path.Combine(isolatedTemp, Guid.NewGuid().ToString() + ".txt");
            var content = new string('a', 1024);
            File.WriteAllText(filePath, content);
            var expectedSize = new FileInfo(filePath).Length;
            Assert.True(File.Exists(filePath));

            var service = new TempCleanupService();

            // Act
            var summary = await service.CleanAsync(isolatedTemp);

            // Assert
            Assert.False(File.Exists(filePath));
            Assert.Equal(1, summary.FilesDeleted);
            Assert.Equal(expectedSize, summary.BytesDeleted);
            Assert.Equal(0, summary.FilesSkipped);
        }

        [Fact]
        public async Task CleanAsync_SkipsLockedFilesGracefully()
        {
            // Arrange: create isolated temp directory and locked file
            var isolatedTemp = Path.Combine(Path.GetTempPath(), "TempCleanupTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(isolatedTemp);
            var lockedFile = Path.Combine(isolatedTemp, Guid.NewGuid().ToString() + ".txt");
            using (var stream = new FileStream(lockedFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                var service = new TempCleanupService();
                var summary = await service.CleanAsync(isolatedTemp);
                Assert.True(File.Exists(lockedFile)); // still exists
                Assert.Equal(0, summary.FilesDeleted);
                Assert.Equal(0, summary.BytesDeleted);
                Assert.True(summary.FilesSkipped >= 1);
            }
            // Cleanup after lock released
            File.Delete(lockedFile);
            Directory.Delete(isolatedTemp, true);
        }

        [Fact]
        public async Task CleanAsync_ObservesCancellationToken()
        {
            var isolatedTemp = Path.Combine(Path.GetTempPath(), "TempCleanupTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(isolatedTemp);
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var service = new TempCleanupService();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await service.CleanAsync(isolatedTemp, cts.Token));
            Directory.Delete(isolatedTemp, true);
        }
    }
}
