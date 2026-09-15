using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class DirectoryScannerTests
    {
        private string CreateTempStructure(out int expectedFileCount)
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(root);

            // create files and subfolders
            expectedFileCount = 0;
            for (int i = 0; i < 3; i++)
            {
                var sub = Path.Combine(root, $"sub{i}");
                Directory.CreateDirectory(sub);
                for (int j = 0; j < 2; j++)
                {
                    var file = Path.Combine(sub, $"file{j}.txt");
                    File.WriteAllText(file, "test");
                    expectedFileCount++;
                }
            }
            return root;
        }

        [Fact]
        public async Task EnumerateFiles_RecursesAndFindsAllFiles()
        {
            var root = CreateTempStructure(out int expected);
            var scanner = new FileScanner();
            var records = new List<FileRecord>();
            await foreach (var rec in scanner.EnumerateFilesAsync(root))
            {
                records.Add(rec);
            }
            Assert.Equal(expected, records.Count);
            // Clean up
            Directory.Delete(root, true);
        }

        [Fact]
        public async Task EnumerateFiles_CancellationIsObserved()
        {
            var root = CreateTempStructure(out int _);
            var scanner = new FileScanner();
            var cts = new CancellationTokenSource();
            int enumerated = 0;
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (var rec in scanner.EnumerateFilesAsync(root, cts.Token))
                {
                    enumerated++;
                    if (enumerated >= 2)
                    {
                        cts.Cancel();
                    }
                }
            });
            // At least one item should have been returned before cancellation
            Assert.True(enumerated > 0);
            Directory.Delete(root, true);
        }

        [Fact]
        public async Task EnumerateFiles_SkipsInaccessibleDirectories()
        {
            var root = CreateTempStructure(out int expected);
            // create an extra subdirectory and then delete it to simulate inaccessibility
            var badSub = Path.Combine(root, "badSub");
            Directory.CreateDirectory(badSub);
            Directory.Delete(badSub);
            var scanner = new FileScanner();
            var records = new List<FileRecord>();
            await foreach (var rec in scanner.EnumerateFilesAsync(root))
            {
                records.Add(rec);
            }
            // Should still get the expected number of files, no exception thrown
            Assert.Equal(expected, records.Count);
            Directory.Delete(root, true);
        }
    }
}
