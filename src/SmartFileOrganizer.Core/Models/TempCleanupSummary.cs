using System;

namespace SmartFileOrganizer.Core.Models
{
    public class TempCleanupSummary
    {
        /// <summary>
        /// Total number of bytes successfully deleted.
        /// </summary>
        public long BytesDeleted { get; set; }

        /// <summary>
        /// Count of files that were deleted.
        /// </summary>
        public int FilesDeleted { get; set; }

        /// <summary>
        /// Count of files that were skipped (locked, in‑use, or access‑denied).
        /// </summary>
        public int FilesSkipped { get; set; }

        /// <summary>
        /// Optional error message if the cleanup terminated early.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
