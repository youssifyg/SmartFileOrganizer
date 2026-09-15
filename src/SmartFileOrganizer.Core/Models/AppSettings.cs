using System.Collections.Generic;

namespace SmartFileOrganizer.Core.Models
{
    public class AppSettings
    {
        public int Id { get; set; }
        // Essential
        public List<string> DefaultScanFolders { get; set; } = new List<string>();
        public List<string> ExcludedFolders { get; set; } = new List<string>();
        public List<string> ExcludedExtensions { get; set; } = new List<string>();
        public bool MoveToRecycleBinByDefault { get; set; } = true;
        public bool AllowPermanentDelete { get; set; } = false;
        public bool ConfirmBeforeEachOperation { get; set; } = true;
        public long MinimumFileSizeBytes { get; set; } = 0;
        public long MaximumFileSizeBytes { get; set; } = 0;

        // Important
        public Dictionary<string, string> CategoryExtensionMap { get; set; } = new Dictionary<string, string>
        {
            { ".jpg", "Images" }, { ".jpeg", "Images" }, { ".png", "Images" }, { ".gif", "Images" }, { ".webp", "Images" },
            { ".doc", "Documents" }, { ".docx", "Documents" }, { ".pdf", "Documents" }, { ".txt", "Documents" }, { ".xls", "Documents" }, { ".xlsx", "Documents" },
            { ".mp4", "Videos" }, { ".mkv", "Videos" }, { ".avi", "Videos" },
            { ".mp3", "Sounds" }, { ".wav", "Sounds" }
        };
        public bool IncludeHiddenFiles { get; set; } = false;
        public bool IncludeSystemFiles { get; set; } = false;
        public ParallelismLevel ParallelismLevel { get; set; } = ParallelismLevel.Balanced;
        public string Language { get; set; } = "English";
        public string Theme { get; set; } = "Light";
        public bool AutoScanEnabled { get; set; } = false;
        public int HistoryRetentionDays { get; set; } = 0;
        public bool MinimizeToTray { get; set; } = false;
        
        // Phase 13
        public float ImageSimilarityThreshold { get; set; } = 0.90f;
        public List<string> ExcludedDirectories { get; set; } = new List<string>();
        public bool DarkMode { get; set; } = true;
    }
}
