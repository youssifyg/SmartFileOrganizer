using SmartFileOrganizer.Core.Models;
using System.Threading.Tasks;

namespace SmartFileOrganizer.Core.Interfaces
{
    public interface ISettingsRepository
    {
        Task<AppSettings> LoadAsync();
        Task SaveAsync( AppSettings settings );
    }
}
