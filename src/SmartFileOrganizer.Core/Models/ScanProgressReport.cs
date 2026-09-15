using System;

namespace SmartFileOrganizer.Core.Models
{
    public class ScanProgressReport
    {
        public int FilesDiscovered { get; set; }
        public int FilesAnalyzed { get; set; }
        public string CurrentFile { get; set; }
        public string Stage { get; set; }
        public double Percentage { get; set; }
    }
}
