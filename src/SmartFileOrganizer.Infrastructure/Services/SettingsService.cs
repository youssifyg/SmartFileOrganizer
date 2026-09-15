using System;
using Microsoft.Extensions.DependencyInjection;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AppSettings Current { get; private set; } = new AppSettings();

        public SettingsService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void Load()
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
                    Current = repo.LoadAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {
                Current = new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var repo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
                    repo.SaveAsync( settings ).GetAwaiter().GetResult();
                }
                Current = settings;
            }
            catch (Exception)
            {
                // Ignore failures for now; in a real app you'd log.
            }
        }
    }
}
