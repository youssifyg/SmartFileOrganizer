using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Application.Services
{
    public class SimilarityScannerService : ISimilarityScannerService
    {
        private readonly IEnumerable<ISimilarityEngine> _engines;

        public SimilarityScannerService( IEnumerable<ISimilarityEngine> engines )
        {
            _engines = engines ?? throw new ArgumentNullException( nameof( engines ) );
        }

        public async Task<IReadOnlyList<SimilarityGroup>> DetectSimilarFilesAsync( IEnumerable<FileRecord> records, CancellationToken cancellationToken = default )
        {
            var groups = new List<SimilarityGroup>();
            var processed = new HashSet<string>();
            var recordList = records.ToList();

            for ( int i = 0; i < recordList.Count; i++ )
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileA = recordList[ i ];
                if ( processed.Contains( fileA.Path ) )
                    continue;

                var extA = Path.GetExtension( fileA.Path );
                var engine = _engines.FirstOrDefault( e => e.SupportsFileType( extA ) );
                if ( engine == null )
                    continue;

                var currentGroupFiles = new List<FileRecord> { fileA };
                processed.Add( fileA.Path );

                for ( int j = i + 1; j < recordList.Count; j++ )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileB = recordList[ j ];
                    if ( processed.Contains( fileB.Path ) )
                        continue;

                    var extB = Path.GetExtension( fileB.Path );
                    if ( !string.Equals( extA, extB, StringComparison.OrdinalIgnoreCase ) )
                        continue;

                    float similarity = await engine.CalculateSimilarityAsync( fileA.Path, fileB.Path, cancellationToken );
                    
                    if ( similarity >= 0.90f )
                    {
                        currentGroupFiles.Add( fileB );
                        processed.Add( fileB.Path );
                    }
                }

                if ( currentGroupFiles.Count > 1 )
                {
                    groups.Add( new SimilarityGroup { Files = currentGroupFiles } );
                }
            }

            return groups;
        }
    }
}
