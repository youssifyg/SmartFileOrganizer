using System;
using System.IO;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure.Services;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class DriveRelocationServiceTests
    {
        private string CreateTempFile(string dir, out long size)
        {
            var filePath = Path.Combine(dir, Guid.NewGuid().ToString() + ".txt");
            var content = new string('a', 1024); // 1 KB
            File.WriteAllText(filePath, content);
            size = new FileInfo(filePath).Length;
            return filePath;
        }

        [Fact]
        public async Task RelocateAsync_EnforcesPathGuard()
        {
            var service = new DriveRelocationService();
            var prohibited = "C:\\Windows";
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.RelocateAsync(prohibited, Path.GetTempPath()));
        }

        [Fact]
        public async Task RelocateAsync_SuccessfulMoveAndResult()
        {
            var source = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destination);

            // create two files
            long size1, size2;
            var f1 = CreateTempFile(source, out size1);
            var f2 = CreateTempFile(source, out size2);

            var service = new DriveRelocationService();
            var result = await service.RelocateAsync(source, destination);

            // destination should contain both files
            Assert.True(File.Exists(Path.Combine(destination, Path.GetFileName(f1))));
            Assert.True(File.Exists(Path.Combine(destination, Path.GetFileName(f2))));
            // source should be empty
            Assert.Empty(Directory.GetFiles(source, "*", SearchOption.AllDirectories));
            // result counts
            Assert.Equal(2, result.FilesProcessed);
            Assert.Equal(size1 + size2, result.BytesProcessed);
            Assert.Equal(0, result.FilesSkipped);
        }

        [Fact]
        public async Task RelocateAsync_SkipsLockedFilesGracefully()
        {
            var source = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var destination = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(destination);

            var lockedFile = Path.Combine(source, "locked.txt");
            // create locked file with exclusive access
            using (var stream = new FileStream(lockedFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                var service = new DriveRelocationService();
                var result = await service.RelocateAsync(source, destination);
                // locked file should still exist and be counted as skipped
                Assert.True(File.Exists(lockedFile));
                Assert.Equal(0, result.FilesProcessed);
                Assert.True(result.FilesSkipped >= 1);
            }
            // cleanup
            File.Delete(lockedFile);
        }
    }
}
