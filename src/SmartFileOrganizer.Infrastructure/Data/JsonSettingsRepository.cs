using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure.Data
{
    public class JsonSettingsRepository : ISettingsRepository
    {
        private readonly string _filePath;

        public JsonSettingsRepository()
        {
            var appData = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
            var folderPath = Path.Combine( appData, "SmartFileOrganizer" );
            
            if ( !Directory.Exists( folderPath ) )
            {
                Directory.CreateDirectory( folderPath );
            }
            
            _filePath = Path.Combine( folderPath, "settings.json" );
        }

        public async Task<AppSettings> LoadAsync()
        {
            if ( !File.Exists( _filePath ) )
            {
                return new AppSettings();
            }

            try
            {
                using var stream = new FileStream( _filePath, FileMode.Open, FileAccess.Read, FileShare.Read );
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>( stream );
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public async Task SaveAsync( AppSettings settings )
        {
            using var stream = new FileStream( _filePath, FileMode.Create, FileAccess.Write, FileShare.None );
            await JsonSerializer.SerializeAsync( stream, settings, new JsonSerializerOptions { WriteIndented = true } );
        }
    }
}
