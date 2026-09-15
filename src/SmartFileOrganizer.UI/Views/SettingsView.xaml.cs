using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using SmartFileOrganizer.Core.Models;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.UI.Themes;

namespace SmartFileOrganizer.UI.Views
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        private readonly ISettingsService _settingsService;

        public SettingsView()
        {
            InitializeComponent();
            var app = (App)System.Windows.Application.Current;
            _settingsService = (ISettingsService)app.ServiceProvider.GetService(typeof(ISettingsService));
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await System.Threading.Tasks.Task.Yield();
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            var settings = _settingsService.Current;

            ExcludedExtensionsBox.Text = string.Join(", ", settings.ExcludedExtensions ?? new List<string>());
            ExcludedFoldersBox.Text = string.Join(", ", settings.ExcludedFolders ?? new List<string>());
            MinSizeBox.Text = settings.MinimumFileSizeBytes.ToString();
            MaxSizeBox.Text = settings.MaximumFileSizeBytes.ToString();
            RecycleBinCheck.IsChecked = settings.MoveToRecycleBinByDefault;
            ConfirmCheck.IsChecked = settings.ConfirmBeforeEachOperation;
            PermanentDeleteCheck.IsChecked = settings.AllowPermanentDelete;

            IncludeHiddenCheck.IsChecked = settings.IncludeHiddenFiles;
            IncludeSystemCheck.IsChecked = settings.IncludeSystemFiles;

            // Phase S4 mappings
            if (AutoScanCheck != null) AutoScanCheck.IsChecked = settings.AutoScanEnabled;
            if (MinimizeTrayCheck != null) MinimizeTrayCheck.IsChecked = settings.MinimizeToTray;
            if (HistoryDaysBox != null) HistoryDaysBox.Text = settings.HistoryRetentionDays.ToString();

            if (LanguageComboBox != null && settings.Language != null)
            {
                foreach (ComboBoxItem item in LanguageComboBox.Items)
                {
                    if ((string)item.Content == settings.Language)
                    {
                        LanguageComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (ThemeComboBox != null && settings.Theme != null)
            {
                foreach (ComboBoxItem item in ThemeComboBox.Items)
                {
                    if ((string)item.Content == settings.Theme)
                    {
                        ThemeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (ParallelismComboBox != null)
            {
                foreach (ComboBoxItem item in ParallelismComboBox.Items)
                {
                    if ((string)item.Content == settings.ParallelismLevel.ToString())
                    {
                        ParallelismComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (CategoryMapGrid != null && settings.CategoryExtensionMap != null)
            {
                var collection = new ObservableCollection<CategoryMapping>();
                foreach (var kv in settings.CategoryExtensionMap)
                {
                    collection.Add(new CategoryMapping { Extension = kv.Key, Category = kv.Value });
                }
                CategoryMapGrid.ItemsSource = collection;
            }
        }

        public void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = _settingsService.Current;

                settings.ExcludedExtensions = ExcludedExtensionsBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                settings.ExcludedFolders = ExcludedFoldersBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

                if (long.TryParse(MinSizeBox.Text, out long minSize)) settings.MinimumFileSizeBytes = minSize;
                if (long.TryParse(MaxSizeBox.Text, out long maxSize)) settings.MaximumFileSizeBytes = maxSize;

                settings.MoveToRecycleBinByDefault = RecycleBinCheck.IsChecked == true;
                settings.ConfirmBeforeEachOperation = ConfirmCheck.IsChecked == true;
                settings.AllowPermanentDelete = PermanentDeleteCheck.IsChecked == true;

                settings.IncludeHiddenFiles = IncludeHiddenCheck.IsChecked == true;
                settings.IncludeSystemFiles = IncludeSystemCheck.IsChecked == true;

                // Phase S4 mappings
                settings.AutoScanEnabled = AutoScanCheck.IsChecked == true;
                settings.MinimizeToTray = MinimizeTrayCheck.IsChecked == true;
                if (int.TryParse(HistoryDaysBox.Text, out int days))
                    settings.HistoryRetentionDays = days;

                if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
                    settings.Language = langItem.Content?.ToString() ?? "English";

                if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
                    settings.Theme = themeItem.Content?.ToString() ?? "Light";

                if (ParallelismComboBox.SelectedItem is ComboBoxItem parItem &&
                    Enum.TryParse<ParallelismLevel>(parItem.Content?.ToString(), out var level))
                {
                    settings.ParallelismLevel = level;
                }

                if (CategoryMapGrid.ItemsSource is IEnumerable<CategoryMapping> items)
                {
                    var map = new Dictionary<string, string>();
                    foreach (var mapping in items)
                    {
                        if (!string.IsNullOrWhiteSpace(mapping.Extension) && !string.IsNullOrWhiteSpace(mapping.Category))
                        {
                            map[mapping.Extension.Trim()] = mapping.Category.Trim();
                        }
                    }
                    settings.CategoryExtensionMap = map;
                }

                _settingsService.Save(settings);

                // Apply theme and language immediately
                ThemeManager.ApplyThemeAndLanguage(settings.Theme, settings.Language);

                var msgBody = global::System.Windows.Application.Current.TryFindResource("StrMsgSettingsSavedBody") as string ?? "Settings saved successfully!";
                var msgTitle = global::System.Windows.Application.Current.TryFindResource("StrMsgSettingsSavedTitle") as string ?? "Settings";
                MessageBox.Show(msgBody, msgTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddCategoryMap_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryMapGrid.ItemsSource is ObservableCollection<CategoryMapping> collection)
                collection.Add(new CategoryMapping { Extension = ".ext", Category = "Other" });
        }

        public void RemoveCategoryMap_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryMapGrid.SelectedItem is CategoryMapping selected &&
                CategoryMapGrid.ItemsSource is ObservableCollection<CategoryMapping> collection)
                collection.Remove(selected);
        }

        public void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var msgBody = global::System.Windows.Application.Current.TryFindResource("StrMsgHistoryClearedBody") as string ?? "History cleared successfully!";
            var msgTitle = global::System.Windows.Application.Current.TryFindResource("StrMsgHistoryClearedTitle") as string ?? "Maintenance";
            MessageBox.Show(msgBody, msgTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var confirmMsg = global::System.Windows.Application.Current.Resources["StrConfirmReset"] as string
                             ?? "Are you sure you want to reset all settings to defaults?";
            var title = global::System.Windows.Application.Current.Resources["StrSettingsTitle"] as string ?? "Settings";

            var result = MessageBox.Show(confirmMsg, title,
                                         MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var newSettings = new SmartFileOrganizer.Core.Models.AppSettings();
                
                await System.Threading.Tasks.Task.Run( () =>
                {
                    _settingsService.Save( newSettings );
                    _settingsService.Load();
                } );
                
                LoadSettingsToUI();

                SmartFileOrganizer.UI.Themes.ThemeManager.ApplyThemeAndLanguage(
                    newSettings.Theme, newSettings.Language );
            }
        }
    }

    public class CategoryMapping
    {
        public string Extension { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
