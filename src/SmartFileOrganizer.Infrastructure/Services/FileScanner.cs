using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class FileScanner : IFileScanner
    {
        private readonly ISettingsService _settingsService;

        public FileScanner(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public async IAsyncEnumerable<FileRecord> EnumerateFilesAsync(string rootPath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Root path must be provided", nameof(rootPath));

            var settings = _settingsService.Current;
            await foreach (var path in ScanAsync(rootPath, cancellationToken))
            {
                // Actively observe cancellation.
                cancellationToken.ThrowIfCancellationRequested();
                var info = new FileInfo(path);
                // Apply settings filters
                if (settings.ExcludedFolders.Any(f => path.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var ext = Path.GetExtension(path);
                if (settings.ExcludedExtensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (settings.MinimumFileSizeBytes > 0 && info.Length < settings.MinimumFileSizeBytes)
                    continue;
                if (settings.MaximumFileSizeBytes > 0 && info.Length > settings.MaximumFileSizeBytes)
                    continue;

                yield return new FileRecord
                {
                    Path = path,
                    Size = info.Length,
                    Created = info.CreationTimeUtc,
                    Modified = info.LastWriteTimeUtc
                };
                // Observe cancellation after yielding an item
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public async IAsyncEnumerable<string> ScanAsync(string rootPath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("Root path must be provided", nameof(rootPath));

            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                // Actively observe cancellation.
                cancellationToken.ThrowIfCancellationRequested();
                var dir = pending.Pop();
                IEnumerable<string> entries;
                try
                {
                    entries = Directory.EnumerateFileSystemEntries(dir);
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (PathTooLongException) { continue; }
                catch (DirectoryNotFoundException) { continue; }

                    foreach (var entry in entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (Directory.Exists(entry))
                            pending.Push(entry);
                        else if (File.Exists(entry))
                            yield return entry;
                    }
                await Task.Yield();
            }
        }
    }
}
