using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.UI.ViewModels;

namespace SmartFileOrganizer.UI
{
    public partial class MainWindow : Window
    {
        private readonly SolidColorBrush _defaultColor = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#34495E"));
        private readonly SolidColorBrush _activeColor = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFA500"));
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private ISettingsService? _settingsService;

        public MainWindow()
        {
            InitializeComponent();

            var sp = ( ( SmartFileOrganizer.UI.App )System.Windows.Application.Current ).ServiceProvider;
            var mainVm = sp.GetRequiredService<MainViewModel>();
            DataContext = mainVm;
            _settingsService = sp.GetService<ISettingsService>();

            SetupNotifyIcon();
            this.Closing += MainWindow_Closing;
            this.Loaded += MainWindow_Loaded;

            _ = InitializeAsync();
        }

        private void SetupNotifyIcon()
{
    _notifyIcon = new System.Windows.Forms.NotifyIcon();

    try
    {
        string? exePath = Environment.ProcessPath;
        if ( !string.IsNullOrEmpty( exePath ) && System.IO.File.Exists( exePath ) )
        {
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon( exePath );
        }
        else
        {
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }
    }
    catch
    {
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
    }

    _notifyIcon.Text = "Smart File Organizer";
    _notifyIcon.Visible = true;
    _notifyIcon.DoubleClick += ( s, args ) =>
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
    };

    var menu = new System.Windows.Forms.ContextMenuStrip();
    menu.Items.Add( "Exit", null, ( s, args ) => 
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    } );
    _notifyIcon.ContextMenuStrip = menu;
}
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_settingsService != null)
            {
                var settings = _settingsService.Current;
                if (settings.MinimizeToTray)
                {
                    e.Cancel = true;
                    this.Hide();
                }
                else
                {
                    _notifyIcon?.Dispose();
                }
            }
        }

        private async void MainWindow_Loaded( object sender, RoutedEventArgs e )
        {
            this.Loaded -= MainWindow_Loaded;
            
            // Force a render step and then load the initial Dashboard ViewModel
            await System.Threading.Tasks.Task.Yield();
            
            var sp = ( ( SmartFileOrganizer.UI.App )System.Windows.Application.Current ).ServiceProvider;
            var mainVm = (MainViewModel)DataContext;
            mainVm.CurrentViewModel = sp.GetRequiredService<DashboardViewModel>();
            ResetButtonColors();
            DashboardButton.Background = _activeColor;

            UpdateRegistryForAutoStart();
        }

        private void UpdateRegistryForAutoStart()
        {
            if (_settingsService == null) return;
            var settings = _settingsService.Current;
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    string appName = "SmartFileOrganizer";
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    exePath = exePath.Replace(".dll", ".exe"); // Since GetExecutingAssembly().Location returns .dll in .NET Core
                    if (settings.AutoScanEnabled)
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                var app = ( SmartFileOrganizer.UI.App )System.Windows.Application.Current;
                if ( app != null && app.ServiceProvider != null )
                {
                    var repo = app.ServiceProvider.GetRequiredService<ISettingsRepository>();
                    var current = await repo.LoadAsync();
                    
                    // Fire and forget theme loading on UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke( () =>
                    {
                        string theme = string.IsNullOrEmpty( current.Theme ) ? "Light" : current.Theme;
                        string language = string.IsNullOrEmpty( current.Language ) ? "English" : current.Language;
                        SmartFileOrganizer.UI.Themes.ThemeManager.ApplyThemeAndLanguage( theme, language );
                    } );
                }
            }
            catch ( Exception ex )
            {
                System.IO.File.WriteAllText( "init_crash.txt", ex.ToString() );
            }
        }



        private void ResetButtonColors()
        {
            OverviewButton.Background = _defaultColor;
            DashboardButton.Background = _defaultColor;
            SettingsButton.Background = _defaultColor;
        }

        private async void OverviewButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;
            var overviewVm = ((SmartFileOrganizer.UI.App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<OverviewViewModel>();
            await overviewVm.RefreshSummary();
            vm.CurrentViewModel = overviewVm;
            ResetButtonColors();
            OverviewButton.Background = _activeColor;
        }

        private async void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            await System.Threading.Tasks.Task.Yield();
            var vm = (MainViewModel)DataContext;
            vm.CurrentViewModel = ((SmartFileOrganizer.UI.App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<DashboardViewModel>();
            ResetButtonColors();
            DashboardButton.Background = _activeColor;
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await System.Threading.Tasks.Task.Yield();
            var vm = (MainViewModel)DataContext;
            vm.CurrentViewModel = ((SmartFileOrganizer.UI.App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<SettingsViewModel>();
            ResetButtonColors();
            SettingsButton.Background = _activeColor;
        }
    }
}