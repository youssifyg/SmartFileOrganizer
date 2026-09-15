namespace SmartFileOrganizer.Core.Models
{
    public class HashCache
    {
        public long Id { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public System.DateTime LastModified { get; set; }
        public byte[] PartialHash { get; set; }
        public byte[] FullHash { get; set; }
    }
}
