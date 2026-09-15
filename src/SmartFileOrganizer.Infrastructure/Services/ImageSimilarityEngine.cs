using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SmartFileOrganizer.Core.Interfaces;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class ImageSimilarityEngine : ISimilarityEngine
    {
        private static readonly string[] _supportedExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

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
                using var image = Image.Load<L8>( stream );

                // Downscale to 9x8 for dHash
                image.Mutate( x => x.Resize( new ResizeOptions
                {
                    Size = new Size( 9, 8 ),
                    Mode = ResizeMode.Stretch,
                    Sampler = KnownResamplers.Bicubic
                } ) );

                ulong hash = 0;
                int bitIndex = 0;

                for ( int y = 0; y < 8; y++ )
                {
                    for ( int x = 0; x < 8; x++ )
                    {
                        // Compare current pixel with the next pixel to the right
                        byte leftPixel = image[ x, y ].PackedValue;
                        byte rightPixel = image[ x + 1, y ].PackedValue;

                        if ( leftPixel > rightPixel )
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
