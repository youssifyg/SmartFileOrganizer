using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Application.Services
{
    public class BatchOperationService : IBatchOperationService
    {
        private readonly IFileOperationService _fileOperationService;
        private readonly IOperationHistoryRepository _historyRepository;

        public BatchOperationService( IFileOperationService fileOperationService, IOperationHistoryRepository historyRepository )
        {
            _fileOperationService = fileOperationService ?? throw new ArgumentNullException( nameof( fileOperationService ) );
            _historyRepository = historyRepository ?? throw new ArgumentNullException( nameof( historyRepository ) );
        }

        public async Task ExecuteAsync( IEnumerable< FileRecord > files, CancellationToken cancellationToken )
        {
            if ( files == null ) throw new ArgumentNullException( nameof( files ) );

            foreach ( var file in files )
            {
                cancellationToken.ThrowIfCancellationRequested( );

                // Perform the safe delete (Recycle Bin).
                await _fileOperationService.DeleteAsync( file.Path ?? throw new InvalidOperationException( "File path cannot be null." ), cancellationToken );

                // Record the operation in history.
                var entry = new OperationHistoryEntry
                {
                    Action = "Delete",
                    FilePath = file.Path,
                    Timestamp = DateTime.UtcNow,
                    Size = file.Size
                };
                await _historyRepository.AddEntryAsync( entry );
            }
        }
    }
}
