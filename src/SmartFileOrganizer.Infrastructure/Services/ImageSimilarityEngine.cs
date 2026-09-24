using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using SmartFileOrganizer.Core.Interfaces;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class ImageSimilarityEngine : ISimilarityEngine
    {
        private static readonly string[] _supportedExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

        public bool SupportsFileType( string extension )
        {
            if ( string.IsNullOrWhiteSpace( extension ) )
                return false;
            
            return _supportedExtensions.Contains( extension.ToLowerInvariant() );
        }

        public async Task<float> CalculateSimilarityAsync( string sourcePath, string targetPath, CancellationToken cancellationToken = default )
        {
            if ( string.IsNullOrWhiteSpace( sourcePath ) || string.IsNullOrWhiteSpace( targetPath ) )
                return 0.0f;

            try
            {
                var hash1 = await ComputeDHashAsync( sourcePath, cancellationToken );
                var hash2 = await ComputeDHashAsync( targetPath, cancellationToken );

                int distance = ComputeHammingDistance( hash1, hash2 );
                
                // dHash is 64 bits. Similarity is (64 - distance) / 64.
                return ( 64.0f - distance ) / 64.0f;
            }
            catch ( Exception )
            {
                return 0.0f;
            }
        }

        private async Task<ulong> ComputeDHashAsync( string imagePath, CancellationToken cancellationToken )
        {
            return await Task.Run( () =>
            {
                // Open file with FileShare.Read to avoid locking
                using var stream = new FileStream( imagePath, FileMode.Open, FileAccess.Read, FileShare.Read );
                using var original = SKBitmap.Decode( stream );
                if ( original == null ) return 0UL;

                using var resized = original.Resize( new SKImageInfo( 9, 8 ), new SKSamplingOptions( SKFilterMode.Linear ) );
                if ( resized == null ) return 0UL;

                ulong hash = 0;
                int bitIndex = 0;

                for ( int y = 0; y < 8; y++ )
                {
                    for ( int x = 0; x < 8; x++ )
                    {
                        var leftColor = resized.GetPixel( x, y );
                        var rightColor = resized.GetPixel( x + 1, y );

                        int leftLuma = ( leftColor.Red * 299 + leftColor.Green * 587 + leftColor.Blue * 114 ) / 1000;
                        int rightLuma = ( rightColor.Red * 299 + rightColor.Green * 587 + rightColor.Blue * 114 ) / 1000;

                        if ( leftLuma > rightLuma )
                        {
                            hash |= ( 1UL << bitIndex );
                        }
                        
                        bitIndex++;
                    }
                }

                return hash;
            }, cancellationToken );
        }

        private int ComputeHammingDistance( ulong hash1, ulong hash2 )
        {
            ulong xor = hash1 ^ hash2;
            int distance = 0;
            
            while ( xor != 0 )
            {
                distance += 1;
                xor &= ( xor - 1 );
            }
            
            return distance;
        }
    }
}
