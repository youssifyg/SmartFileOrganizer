using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
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

        private string CreateMockImage( string name, int width, int height, SKColor color )
        {
            var path = Path.Combine( _tempDir, name );
            using ( var image = new SKBitmap( width, height ) )
            using ( var canvas = new SKCanvas( image ) )
            {
                canvas.Clear( color );
                var paint = new SKPaint { Color = SKColors.White };
                canvas.DrawRect( new SKRect( 0, 0, width / 2, height / 2 ), paint );

                using var fs = new FileStream( path, FileMode.Create );
                image.Encode( fs, SKEncodedImageFormat.Jpeg, 100 );
            }
            return path;
        }

        [Fact]
        public async Task CalculateSimilarityAsync_IdenticalImages_ReturnsOne()
        {
            string imgPath = CreateMockImage( "test1.jpg", 100, 100, SKColors.Red );

            float similarity = await _engine.CalculateSimilarityAsync( imgPath, imgPath, CancellationToken.None );

            Assert.Equal( 1.0f, similarity );
        }

        [Fact]
        public async Task CalculateSimilarityAsync_ResizedImage_ReturnsHighSimilarity()
        {
            string originalPath = CreateMockImage( "orig.jpg", 100, 100, SKColors.Green );
            string resizedPath = Path.Combine( _tempDir, "resized.jpg" );
            
            using ( var image = SKBitmap.Decode( originalPath ) )
            using ( var resized = image.Resize( new SKImageInfo( 50, 50 ), new SKSamplingOptions( SKFilterMode.Linear ) ) )
            using ( var fs = new FileStream( resizedPath, FileMode.Create ) )
            {
                resized.Encode( fs, SKEncodedImageFormat.Jpeg, 100 );
            }

            float similarity = await _engine.CalculateSimilarityAsync( originalPath, resizedPath, CancellationToken.None );

            Assert.True( similarity >= 0.95f, $"Expected high similarity, got { similarity }" );
        }

        [Fact]
        public async Task CalculateSimilarityAsync_DifferentImages_ReturnsLowSimilarity()
        {
            string img1Path = CreateMockImage( "diff1.jpg", 100, 100, SKColors.Red );
            string img2Path = CreateMockImage( "diff2.jpg", 100, 100, SKColors.Blue );

            // Overwrite img2 to make it completely different visually
            using ( var image = new SKBitmap( 100, 100 ) )
            using ( var canvas = new SKCanvas( image ) )
            {
                canvas.Clear( SKColors.Blue );
                var paint = new SKPaint { Color = SKColors.Black };
                canvas.DrawRect( new SKRect( 50, 50, 100, 100 ), paint );
                
                using var fs = new FileStream( img2Path, FileMode.Create );
                image.Encode( fs, SKEncodedImageFormat.Jpeg, 100 );
            }

            float similarity = await _engine.CalculateSimilarityAsync( img1Path, img2Path, CancellationToken.None );

            Assert.True( similarity < 0.9f, $"Expected low similarity, got { similarity }" );
        }

        [Fact]
        public async Task CalculateSimilarityAsync_CorruptImage_ReturnsZeroSafely()
        {
            string imgPath = CreateMockImage( "valid.jpg", 100, 100, SKColors.Red );
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
