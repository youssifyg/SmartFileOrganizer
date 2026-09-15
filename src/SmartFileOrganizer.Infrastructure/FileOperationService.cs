using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure
{
    public class FileOperationService : IFileOperationService
    {
        private readonly IRepository _repository;

        public FileOperationService(IRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must not be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: { filePath }", filePath);

            var fileInfo = new FileInfo(filePath);
            var size = fileInfo.Length;

            FileSystem.DeleteFile(filePath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.DoNothing);

            var entry = new OperationHistoryEntry
            {
                Action = "Delete",
                FilePath = filePath,
                Timestamp = DateTime.UtcNow,
                Size = size
            };
            await _repository.SaveOperationHistoryAsync(entry, cancellationToken);
        }
    }
}
