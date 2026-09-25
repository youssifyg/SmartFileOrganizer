using System.Windows.Input;
using SmartFileOrganizer.UI.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace SmartFileOrganizer.UI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SmartFileOrganizer.Core.Interfaces.ISettingsService _settingsService;
        
        public string Title { get; set; } = "Settings";

        public int SmartSelectionMode
        {
            get => (int)_settingsService.Current.SmartSelectionMode;
            set
            {
                if ((int)_settingsService.Current.SmartSelectionMode != value)
                {
                    _settingsService.Current.SmartSelectionMode = (SmartFileOrganizer.Core.Models.SmartSelectionMode)value;
                    _settingsService.Save(_settingsService.Current);
                    OnPropertyChanged();
                }
            }
        }

        public ICommand BrowseFolderCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public SettingsViewModel(SmartFileOrganizer.Core.Interfaces.ISettingsService settingsService)
        {
            _settingsService = settingsService;
            BrowseFolderCommand = new RelayCommand( ExecuteBrowseFolder );
            ClearHistoryCommand = new RelayCommand( async _ => await ExecuteClearHistoryAsync() );
        }

        private async System.Threading.Tasks.Task ExecuteClearHistoryAsync()
        {
            var repo = ((SmartFileOrganizer.UI.App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<SmartFileOrganizer.Core.Interfaces.IRepository>();
            await repo.ClearAllDataAsync();
SmartFileOrganizer.Core.Events.GlobalEvents.NotifyHistoryCleared();
            var title = System.Windows.Application.Current.TryFindResource("StrHistoryClearedTitle") as string ?? "History Cleared";
var msg = System.Windows.Application.Current.TryFindResource("StrHistoryClearedMsg") as string ?? "All scanning history and statistics have been permanently cleared.";
System.Windows.MessageBox.Show(msg, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ExecuteBrowseFolder( object? parameter )
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if ( dialog.ShowDialog() == true )
            {
                if ( parameter is System.Windows.Controls.TextBox textBox )
                {
                    if ( string.IsNullOrWhiteSpace( textBox.Text ) )
                    {
                        textBox.Text = dialog.FolderName;
                    }
                    else
                    {
                        textBox.Text += ", " + dialog.FolderName;
                    }
                }
            }
        }
    }
}
