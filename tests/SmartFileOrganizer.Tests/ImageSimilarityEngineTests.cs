using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SmartFileOrganizer.Infrastructure.Services;
using Xunit;

namespace SmartFileOrganizer.Tests
{
    public class ImageSimilarityEngineTests : IDisposable
    {
        private readonly ImageSimilarityEngine _engine;
        private readonly string _tempDir;

        public ImageSimilarityEngineTests()
        {
            _engine = new ImageSimilarityEngine();
            _tempDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
            Directory.CreateDirectory( _tempDir );
        }

        public void Dispose()
        {
            if ( Directory.Exists( _tempDir ) )
            {
                Directory.Delete( _tempDir, true );
            }
        }

        private string CreateMockImage( string name, int width, int height, Rgba32 color )
        {
            var path = Path.Combine( _tempDir, name );
            using ( var image = new Image<Rgba32>( width, height, color ) )
            {
                // Add some arbitrary shape to make it not just a solid color
                for ( int y = 0; y < height / 2; y++ )
                {
                    for ( int x = 0; x < width / 2; x++ )
                    {
                        image[ x, y ] = new Rgba32( 255, 255, 255 );
                    }
                }
                image.SaveAsJpeg( path );
            }
            return path;
        }

        [Fact]
        public async Task CalculateSimilarityAsync_IdenticalImages_ReturnsOne()
        {
            string imgPath = CreateMockImage( "test1.jpg", 100, 100, new Rgba32( 255, 0, 0 ) );

            float similarity = await _engine.CalculateSimilarityAsync( imgPath, imgPath, CancellationToken.None );

            Assert.Equal( 1.0f, similarity );
        }

        [Fact]
        public async Task CalculateSimilarityAsync_ResizedImage_ReturnsHighSimilarity()
        {
            string originalPath = CreateMockImage( "orig.jpg", 100, 100, new Rgba32( 0, 255, 0 ) );
            string resizedPath = Path.Combine( _tempDir, "resized.jpg" );
            
            using ( var image = Image.Load( originalPath ) )
            {
                image.Mutate( x => x.Resize( 50, 50 ) );
                image.SaveAsJpeg( resizedPath );
            }

            float similarity = await _engine.CalculateSimilarityAsync( originalPath, resizedPath, CancellationToken.None );

            Assert.True( similarity >= 0.95f, $"Expected high similarity, got { similarity }" );
        }

        [Fact]
        public async Task CalculateSimilarityAsync_DifferentImages_ReturnsLowSimilarity()
        {
            string img1Path = CreateMockImage( "diff1.jpg", 100, 100, new Rgba32( 255, 0, 0 ) );
            string img2Path = CreateMockImage( "diff2.jpg", 100, 100, new Rgba32( 0, 0, 255 ) );

            // Overwrite img2 to make it completely different visually
            using ( var image = new Image<Rgba32>( 100, 100, new Rgba32( 0, 0, 255 ) ) )
            {
                for ( int y = 50; y < 100; y++ )
                {
                    for ( int x = 50; x < 100; x++ )
                    {
                        image[ x, y ] = new Rgba32( 0, 0, 0 );
                    }
                }
                image.SaveAsJpeg( img2Path );
            }

            float similarity = await _engine.CalculateSimilarityAsync( img1Path, img2Path, CancellationToken.None );

            Assert.True( similarity < 0.9f, $"Expected low similarity, got { similarity }" );
        }

        [Fact]
        public async Task CalculateSimilarityAsync_CorruptImage_ReturnsZeroSafely()
        {
            string imgPath = CreateMockImage( "valid.jpg", 100, 100, new Rgba32( 255, 0, 0 ) );
            string corruptPath = Path.Combine( _tempDir, "corrupt.jpg" );
            
            File.WriteAllText( corruptPath, "Not an image file" );

            float similarity = await _engine.CalculateSimilarityAsync( imgPath, corruptPath, CancellationToken.None );

            Assert.Equal( 0.0f, similarity );
        }

        [Fact]
        public void SupportsFileType_ReturnsExpectedValues()
        {
            Assert.True( _engine.SupportsFileType( ".jpg" ) );
            Assert.True( _engine.SupportsFileType( ".PNG" ) );
            Assert.False( _engine.SupportsFileType( ".txt" ) );
            Assert.False( _engine.SupportsFileType( null ) );
        }
    }
}
