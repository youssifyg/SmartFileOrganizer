namespace SmartFileOrganizer.Core.Models
{
    public class ScanSession
    {
        public int Id { get; set; }
        public System.DateTime StartedAt { get; set; }
        public System.DateTime? CompletedAt { get; set; }
        public int FilesDiscovered { get; set; }
    }
}
