using System.Windows.Input;
using SmartFileOrganizer.UI.Mvvm;

namespace SmartFileOrganizer.UI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        // Placeholder for settings UI
        public string Title { get; set; } = "Settings";

        public ICommand BrowseFolderCommand { get; }

        public SettingsViewModel()
        {
            BrowseFolderCommand = new RelayCommand( ExecuteBrowseFolder );
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
