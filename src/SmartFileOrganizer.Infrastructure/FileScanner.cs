using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Infrastructure.Services;

namespace SmartFileOrganizer.Infrastructure
{
    public class FileScanner : IFileScanner
    {
        private readonly Services.FileScanner _inner;

        // Parameterless constructor for test compatibility – creates a minimal SettingsService with an in‑memory settings repository.
        public FileScanner()
        {
            // Build a simple service provider containing an in‑memory ISettingsRepository.
            var services = new ServiceCollection();
            services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>();
            // Register IServiceScopeFactory via the built provider.
            var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

            // Create a SettingsService using the scope factory.
            var settingsService = new SettingsService(scopeFactory);
            _inner = new Services.FileScanner(settingsService);
        }

        public FileScanner(ISettingsService settingsService)
        {
            _inner = new Services.FileScanner(settingsService);
        }

        // Minimal in‑memory repository used by the parameter‑less constructor.
        private class InMemorySettingsRepository : ISettingsRepository
        {
            public Task<AppSettings> LoadAsync() => Task.FromResult( new AppSettings() );
            public Task SaveAsync( AppSettings settings ) => Task.CompletedTask;
        }

        public async IAsyncEnumerable<FileRecord> EnumerateFilesAsync(string rootPath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var rec in _inner.EnumerateFilesAsync(rootPath, cancellationToken))
            {
                yield return rec;
            }
        }

        public async IAsyncEnumerable<string> ScanAsync(string rootPath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var path in _inner.ScanAsync(rootPath, cancellationToken))
            {
                yield return path;
            }
        }
    }
}

