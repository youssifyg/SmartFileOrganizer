using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class TempCleanupService : ITempCleanupService
    {
        /// <summary>
        /// Cleans the user and system temporary folders.
        /// </summary>
        public async Task<TempCleanupSummary> CleanAsync(string? customTempPath = null, CancellationToken cancellationToken = default)
        {
            var summary = new TempCleanupSummary();
            // Resolve user temp path – use custom path if supplied, otherwise default system temp
            string userTemp;
            if (!string.IsNullOrWhiteSpace(customTempPath))
            {
                userTemp = customTempPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else
            {
                userTemp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            if (!Directory.Exists(userTemp))
                return summary;

            // Delete only files directly under the temp root (non-recursive)
            string[] files;
            try
            {
                // Throw if cancellation requested before starting work
                cancellationToken.ThrowIfCancellationRequested();
                // Get all files recursively to ensure any nested files are cleaned
                files = Directory.GetFiles(userTemp, "*", SearchOption.AllDirectories);
                Console.WriteLine($"[Debug] Found {files.Length} files in {userTemp}");
            }
            catch (IOException)
            {
                return summary; // If we can't read, return empty summary
            }
            catch (UnauthorizedAccessException)
            {
                return summary;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    try
                    {
                        // Ensure we can delete even if file has read‑only or hidden attributes
                        var fileInfo = new FileInfo(file);
                        // Capture size before deletion
                        var size = fileInfo.Length;
                        File.SetAttributes(file, FileAttributes.Normal);
                        // Delete the file using FileInfo
                        fileInfo.Delete();
                        summary.BytesDeleted += size;
                        summary.FilesDeleted++;
                        Console.WriteLine($"[Debug] Deleted {file}");
                    }
                    catch (IOException ex)
                    {
                        // Possible file lock or other I/O issue, count as skipped
                        Console.WriteLine($"[Debug] Delete I/O failed {file}: {ex.Message}");
                        summary.FilesSkipped++;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        // Access denied, count as skipped
                        Console.WriteLine($"[Debug] Delete access denied {file}: {ex.Message}");
                        summary.FilesSkipped++;
                    }
                    catch (Exception ex)
                    {
                        // Unexpected error, count as skipped
                        Console.WriteLine($"[Debug] Delete unexpected error {file}: {ex.GetType().Name} {ex.Message}");
                        summary.FilesSkipped++;
                    }
                }
                catch (IOException)
                {
                    summary.FilesSkipped++;
                }
                catch (UnauthorizedAccessException)
                {
                    summary.FilesSkipped++;
                }
            }

            // Process system temp folder as well only when no custom path is supplied
            var systemTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            bool processSystemTemp = string.IsNullOrWhiteSpace(customTempPath);
            if (processSystemTemp && Directory.Exists(systemTemp))
                await ProcessTempFolderAsync(systemTemp, summary, cancellationToken);

            return summary;
        }

        private async Task ProcessTempFolderAsync(string tempPath, TempCleanupSummary summary, CancellationToken cancellationToken)
        {
            // Delete only files directly under the temp root (non-recursive)
            string[] files;
            try
            {
                files = Directory.GetFiles(tempPath);
            }
            catch (IOException)
            {
                return; // If we can't read, skip
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    File.Delete(file);
                    summary.BytesDeleted += info.Length;
                    summary.FilesDeleted++;
                }
                catch (IOException)
                {
                    summary.FilesSkipped++;
                }
                catch (UnauthorizedAccessException)
                {
                    summary.FilesSkipped++;
                }
            }
        }
        }
    }

