using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SmartFileOrganizer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using SmartFileOrganizer.Infrastructure.Data;
using SmartFileOrganizer.Infrastructure.Data.Repositories;
using SmartFileOrganizer.UI.ViewModels;
using Microsoft.Extensions.Logging;

namespace SmartFileOrganizer.UI
{
    public partial class App : System.Windows.Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        // Central AppData directory (safe, no admin)
        private static readonly string AppDataDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartFileOrganizer");

        public App()
        {
            // Bullet‑proof AppDomain crash logger writing to LocalAppData
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartFileOrganizer_Crash.txt");
                System.IO.File.WriteAllText(logPath, "AppDomain Fatal Error: " + ex?.ToString());
                System.Windows.MessageBox.Show(
                    "Fatal Error. Check crash log in LocalAppData.",
                    "Crash",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                // 1. Show Splash
                var splash = new Views.SplashWindow();
                splash.Show();

                // 2. Start mandatory delay and initialize backend
                var minimumDelay = System.Threading.Tasks.Task.Delay(2500);

                // ==== DI container build (unchanged) ==== //
                var serviceCollection = new ServiceCollection();
                System.IO.Directory.CreateDirectory(AppDataDir);
                string dbPath = System.IO.Path.Combine(AppDataDir, "smartfileorganizer.db");

                serviceCollection.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));
                serviceCollection.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
                serviceCollection.AddScoped<IOperationHistoryRepository, OperationHistoryRepository>();
                serviceCollection.AddScoped<IScanSessionRepository, ScanSessionRepository>();

                // Existing services
                serviceCollection.AddSingleton<SmartFileOrganizer.Core.Interfaces.ISettingsService,
                    SmartFileOrganizer.Infrastructure.Services.SettingsService>();
                serviceCollection.AddTransient<IFileScanner, SmartFileOrganizer.Infrastructure.FileScanner>();
                serviceCollection.AddTransient<SmartFileOrganizer.Core.Interfaces.IFileOperationService,
                    SmartFileOrganizer.Infrastructure.FileOperationService>();

                // New services
                serviceCollection.AddSingleton<ITempCleanupService,
                    SmartFileOrganizer.Infrastructure.Services.TempCleanupService>();
                serviceCollection.AddSingleton<IDriveRelocationService,
                    SmartFileOrganizer.Infrastructure.Services.DriveRelocationService>();
                serviceCollection.AddSingleton<ISimilarityEngine,
                    SmartFileOrganizer.Infrastructure.Services.PlaceholderSimilarityEngine>();
                serviceCollection.AddSingleton<ISimilarityEngine,
                    SmartFileOrganizer.Infrastructure.Services.ImageSimilarityEngine>();
                serviceCollection.AddTransient<ISimilarityScannerService,
                    SmartFileOrganizer.Application.Services.SimilarityScannerService>();

                // Core scanning services
                serviceCollection.AddTransient<SmartFileOrganizer.Core.Interfaces.IHashService,
                    SmartFileOrganizer.Infrastructure.HashService>();
                serviceCollection.AddTransient<SmartFileOrganizer.Application.DuplicateEngine>();
                serviceCollection.AddTransient<SmartFileOrganizer.Application.Services.ScanOrchestrator>();
                serviceCollection.AddScoped<SmartFileOrganizer.Core.Interfaces.IRepository>(sp =>
                {
                    var path = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SmartFileOrganizer",
                        "smartfileorganizer.db");
                    return new SmartFileOrganizer.Infrastructure.SQLiteRepository(path);
                });

                // Logging and cache
                serviceCollection.AddLogging();
                serviceCollection.AddSingleton<SmartFileOrganizer.Core.Models.HashCacheService>();

                // ViewModels and MainWindow
                serviceCollection.AddSingleton<MainViewModel>();
                serviceCollection.AddSingleton<DashboardViewModel>();
                serviceCollection.AddSingleton<OverviewViewModel>();
                serviceCollection.AddSingleton<SettingsViewModel>();
                serviceCollection.AddTransient<MainWindow>();

                ServiceProvider = serviceCollection.BuildServiceProvider();

                // ==== Apply pending DB migrations ==== //
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        using var scope = ServiceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        dbContext.Database.Migrate();
                    }
                    catch (Exception migrationEx)
                    {
                        WriteDebugLog($"Migration failed: {migrationEx.Message}");
                    }
                });

                // ==== Initialise repository ==== //
                var repo = ServiceProvider.GetRequiredService<SmartFileOrganizer.Core.Interfaces.IRepository>();
                await repo.InitializeAsync();

                // ==== Resolve and show MainWindow via DI ==== //
                await minimumDelay;

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
                System.Windows.Application.Current.MainWindow = mainWindow;
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
                splash.Close();
            }
            catch (Exception ex)
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartFileOrganizer_StartupCrash.txt");
                System.IO.File.WriteAllText(logPath, "Startup Error: " + ex.ToString());
                System.Windows.MessageBox.Show(
                    "Startup Error. Check crash log in LocalAppData.",
                    "Crash",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        // Helper debug logger (writes to the same AppData folder)
        private static void WriteDebugLog(string message)
        {
            try
            {
                System.IO.Directory.CreateDirectory(AppDataDir);
                string logPath = System.IO.Path.Combine(AppDataDir, "startup_debug.txt");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                System.IO.File.AppendAllText(logPath, $"[{timestamp}] {message}\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write debug log: {ex.Message}");
            }
        }
    }
}
