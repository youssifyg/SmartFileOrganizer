using System;

namespace SmartFileOrganizer.Core.Models
{
    /// <summary>
    /// Represents the result of a drive relocation or delete operation.
    /// </summary>
    public class RelocationResult
    {
        /// <summary>Total number of bytes processed (copied or deleted).</summary>
        public long BytesProcessed { get; set; }

        /// <summary>Total number of files successfully processed.</summary>
        public int FilesProcessed { get; set; }

        /// <summary>Number of files that were skipped due to errors (e.g., locked, access denied).</summary>
        public int FilesSkipped { get; set; }

        /// <summary>Optional error message when the operation failed or was partially successful.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Indicates success when no error message is set.</summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
    }
}
