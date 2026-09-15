// ISettingsProvider.cs
namespace SmartFileOrganizer.Core.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISettingsProvider
    {
        Task<IReadOnlyList<string>> GetPreferredFoldersAsync();
        Task<IReadOnlyList<string>> GetIgnoredFoldersAsync();
        Task SavePreferredFoldersAsync(IReadOnlyList<string> folders);
        Task SaveIgnoredFoldersAsync(IReadOnlyList<string> folders);
    }
}
