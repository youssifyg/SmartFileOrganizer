using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileOrganizer.Core.Interfaces;

namespace SmartFileOrganizer.Infrastructure.Services
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly List<string> _preferredFolders = new();
        private readonly List<string> _ignoredFolders = new();

        public Task<IReadOnlyList<string>> GetPreferredFoldersAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(_preferredFolders);
        }

        public Task<IReadOnlyList<string>> GetIgnoredFoldersAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(_ignoredFolders);
        }

        public Task SavePreferredFoldersAsync(IReadOnlyList<string> folders)
        {
            _preferredFolders.Clear();
            _preferredFolders.AddRange(folders);
            return Task.CompletedTask;
        }

        public Task SaveIgnoredFoldersAsync(IReadOnlyList<string> folders)
        {
            _ignoredFolders.Clear();
            _ignoredFolders.AddRange(folders);
            return Task.CompletedTask;
        }
    }
}
