namespace SmartFileOrganizer.Core.Models
{
    public class OperationHistoryEntry
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string FilePath { get; set; }
        public System.DateTime Timestamp { get; set; }
        public long Size { get; set; }
    }
}
