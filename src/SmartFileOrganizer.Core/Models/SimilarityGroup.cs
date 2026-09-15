using System.Collections.Generic;

namespace SmartFileOrganizer.Core.Models
{
    public class SimilarityGroup
    {
        public IReadOnlyList<FileRecord> Files { get; set; } = new List<FileRecord>();
    }
}
