using System.Collections.Generic;

namespace SmartFileOrganizer.Core.Interfaces
{
    using SmartFileOrganizer.Core.Models;

    public interface ISettingsService
    {
        AppSettings Current { get; }
        void Load();
        void Save(AppSettings settings);
    }
}
