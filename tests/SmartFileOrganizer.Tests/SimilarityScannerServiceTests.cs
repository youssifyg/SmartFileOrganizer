using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartFileOrganizer.Application.Services;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class SimilarityScannerServiceTests
    {
        private class MockEngine : ISimilarityEngine
        {
            public bool SupportsFileType( string extension ) => extension == ".jpg";

            public Task<float> CalculateSimilarityAsync( string sourcePath, string targetPath, CancellationToken cancellationToken = default )
            {
                // Simple mock logic: if filenames end with the same letter before extension, they are similar.
                // e.g. fileA.jpg and diffA.jpg are 1.0f.
                // e.g. fileA.jpg and fileB.jpg are 0.0f.
                var aChar = sourcePath.Replace( ".jpg", "" ).LastOrDefault();
                var bChar = targetPath.Replace( ".jpg", "" ).LastOrDefault();

                if ( aChar == bChar )
                {
                    return Task.FromResult( 1.0f );
                }
                
                return Task.FromResult( 0.0f );
            }
        }

        [Fact]
        public async Task DetectSimilarFilesAsync_GroupsSimilarFilesCorrectly()
        {
            var engines = new List<ISimilarityEngine> { new MockEngine() };
            var service = new SimilarityScannerService( engines );

            var records = new List<FileRecord>
            {
                new FileRecord { Path = "imageA.jpg" },
                new FileRecord { Path = "photoA.jpg" },
                new FileRecord { Path = "imageB.jpg" },
                new FileRecord { Path = "photoB.jpg" },
                new FileRecord { Path = "imageC.jpg" },
                new FileRecord { Path = "ignored.txt" } // Engine doesn't support .txt
            };

            var groups = await service.DetectSimilarFilesAsync( records );

            // Expected groups:
            // Group 1: imageA.jpg, photoA.jpg
            // Group 2: imageB.jpg, photoB.jpg
            // imageC.jpg has no matches, so it doesn't form a group.
            // ignored.txt is not supported.
            
            Assert.Equal( 2, groups.Count );
            
            var groupA = groups.FirstOrDefault( g => g.Files.Any( f => f.Path == "imageA.jpg" ) );
            Assert.NotNull( groupA );
            Assert.Equal( 2, groupA.Files.Count );

            var groupB = groups.FirstOrDefault( g => g.Files.Any( f => f.Path == "imageB.jpg" ) );
            Assert.NotNull( groupB );
            Assert.Equal( 2, groupB.Files.Count );
        }
    }
}
