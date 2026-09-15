using System;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface IDriveRelocationService
    {
        Task<RelocationResult> RelocateAsync( string sourcePath, string destinationPath, CancellationToken cancellationToken = default );
        Task<RelocationResult> DeleteFromDriveAsync( string path, bool permanent = false, CancellationToken cancellationToken = default );
    }
}
