using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure;
using SmartFileOrganizer.Application;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class DuplicateEngineTests
    {
        private string CreateTempFile(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
            File.WriteAllText(path, content);
            return path;
        }

        private void Cleanup(params string[] paths)
        {
            foreach (var p in paths)
                if (File.Exists(p)) File.Delete(p);
        }

        [Fact]
        public async Task FilesWithSameSizeDifferentContent_AreNotGrouped()
        {
            var file1 = CreateTempFile("AAA"); // 3 bytes
            var file2 = CreateTempFile("BBB"); // same length 3 bytes but different content
            var records = new List<FileRecord>
            {
                new FileRecord { Path = file1, Size = new FileInfo(file1).Length },
                new FileRecord { Path = file2, Size = new FileInfo(file2).Length }
            };

            var hashService = new HashService();
            var engine = new DuplicateEngine(hashService);
            var result = await engine.DetectDuplicatesAsync(records);

            Assert.Empty(result);
            Cleanup(file1, file2);
        }

        [Fact]
        public async Task IdenticalFiles_DifferentNames_AreGrouped()
        {
            var content = "SameContent";
            var file1 = CreateTempFile(content);
            var file2 = CreateTempFile(content);
            var records = new List<FileRecord>
            {
                new FileRecord { Path = file1, Size = new FileInfo(file1).Length },
                new FileRecord { Path = file2, Size = new FileInfo(file2).Length }
            };

            var hashService = new HashService();
            var engine = new DuplicateEngine(hashService);
            var result = await engine.DetectDuplicatesAsync(records);

            Assert.Single(result);
            var group = result.First();
            Assert.Equal(2, group.Files.Count);
            Assert.Equal(group.TotalSize, group.Files.Sum(r => r.Size));
            Assert.Equal(group.TotalSize - group.Files.First().Size, group.RecoverableSize);
            Cleanup(file1, file2);
        }

        [Fact]
        public async Task PartialAndFullHash_WorkCorrectly()
        {
            // Create a larger file (>4KB) to ensure partial hash reads only a chunk.
            var largeContent = new string('X', 10_000);
            var file = CreateTempFile(largeContent);
            var records = new List<FileRecord> { new FileRecord { Path = file, Size = new FileInfo(file).Length } };
            var hashService = new HashService();

            var partial = await hashService.CalculatePartialHashAsync(file, chunkSize: 4096);
            var full = await hashService.CalculateFullHashAsync(file);

            // Partial hash should be shorter than full hash, but both non‑null and length 32 (SHA‑256)
            Assert.NotNull(partial);
            Assert.NotNull(full);
            Assert.Equal(32, partial.Length);
            Assert.Equal(32, full.Length);
            // Full hash must differ from partial hash for this data (unlikely to be equal)
            Assert.NotEqual(Convert.ToHexString(partial), Convert.ToHexString(full));
            Cleanup(file);
        }
    }
}
