using System.Collections.Generic;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Models
{
    public class DuplicateGroup
    {
        public int Id { get; set; }
        public IReadOnlyList<FileRecord> Files { get; init; }
        public long TotalSize { get; init; }
        public long RecoverableSize { get; init; }
    }
}
