using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure.Services
{
    /// <summary>
    /// Implements drive relocation and deletion with safety guards, free‑space checks,
    /// transactional copy‑verify‑delete, cancellation support, and try‑catch‑skip for locked files.
    /// </summary>
    public class DriveRelocationService : IDriveRelocationService
    {
        private static readonly string[] ProhibitedPrefixes =
        {
            "C:\\Windows",
            "C:\\Program Files",
            "C:\\Program Files (x86)",
            "C:\\Boot",
            "C:\\Recovery",
            "C:\\System Volume Information"
        };

        private void ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Path cannot be null or whitespace.");

            foreach (var prefix in ProhibitedPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Operation on prohibited system path '{path}' is not allowed.");
            }
        }

        public async Task<RelocationResult> RelocateAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        {
            // Normalize paths (remove trailing directory separators) for deterministic behavior
            sourcePath = sourcePath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            destinationPath = destinationPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            ValidatePath(sourcePath);
            ValidatePath(destinationPath);

            var result = new RelocationResult();

            if (!Directory.Exists(sourcePath))
            {
                // Source directory missing – nothing to relocate; return empty result
                return result;
            }

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            // Gather all files to process using an Allow-List and System/Hidden filters
            var safeExtensions = new System.Collections.Generic.HashSet<string>( System.StringComparer.OrdinalIgnoreCase ) { ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".xls", ".docx", ".doc", ".txt", ".csv", ".mp4", ".mp3" };
            var files = new System.IO.DirectoryInfo( sourcePath )
                .EnumerateFiles( "*", System.IO.SearchOption.AllDirectories )
                .Where( f => ( f.Attributes & ( System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System ) ) == 0 && safeExtensions.Contains( f.Extension ) )
                .Select( f => f.FullName )
                .ToList();

            // Pre‑flight free‑space check on destination drive
            var destRoot = Path.GetPathRoot(destinationPath) ?? throw new InvalidOperationException("Destination path has no root.");
            var driveInfo = new DriveInfo(destRoot);
            var totalSize = files.Sum(f => new FileInfo(f).Length);
            if (driveInfo.AvailableFreeSpace < totalSize)
            {
                result.ErrorMessage = "Insufficient free space on destination drive.";
                return result;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relative = Path.GetRelativePath(sourcePath, file);
                var destFile = Path.Combine(destinationPath, relative);
                var destDir = Path.GetDirectoryName(destFile) ?? destinationPath;
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                try
                {
                    File.Copy(file, destFile, overwrite: true);
                    // Verify copy size matches source
                    var srcInfo = new FileInfo(file);
                    var destInfo = new FileInfo(destFile);
                    if (srcInfo.Length != destInfo.Length)
                        throw new IOException("File size mismatch after copy.");

                    // Delete source file after successful copy
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // Locked or in‑use source file – skip deletion
                        result.FilesSkipped++;
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        result.FilesSkipped++;
                        continue;
                    }

                    result.BytesProcessed += srcInfo.Length;
                    result.FilesProcessed++;
                }
                catch (IOException)
                {
                    // Copy failed – count as skipped and continue
                    result.FilesSkipped++;
                }
                catch (UnauthorizedAccessException)
                {
                    result.FilesSkipped++;
                }
            }

            await Task.CompletedTask; // Preserve async signature
            return result;
        }

        public async Task<RelocationResult> DeleteFromDriveAsync(string path, bool permanent = false, CancellationToken cancellationToken = default)
        {
            ValidatePath(path);
            var result = new RelocationResult();

            if (!Directory.Exists(path))
                return result; // Nothing to delete

            var safeExtensions = new System.Collections.Generic.HashSet<string>( System.StringComparer.OrdinalIgnoreCase ) { ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".xls", ".docx", ".doc", ".txt", ".csv", ".mp4", ".mp3" };
            var files = new System.IO.DirectoryInfo( path )
                .EnumerateFiles( "*", System.IO.SearchOption.AllDirectories )
                .Where( f => ( f.Attributes & ( System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System ) ) == 0 && safeExtensions.Contains( f.Extension ) )
                .Select( f => f.FullName )
                .ToList();
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(file);
                    File.Delete(file);
                    result.BytesProcessed += info.Length;
                    result.FilesProcessed++;
                }
                catch (IOException)
                {
                    result.FilesSkipped++;
                }
                catch (UnauthorizedAccessException)
                {
                    result.FilesSkipped++;
                }
            }

            // Optionally, remove empty directories after file deletions
            try
            {
                var dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                foreach (var dir in dirs.OrderByDescending(d => d.Length))
                {
                    try { Directory.Delete(dir, false); } catch { /* ignore */ }
                }
                Directory.Delete(path, false);
            }
            catch { /* ignore any failures */ }

            await Task.CompletedTask;
            return result;
        }
    }
}
