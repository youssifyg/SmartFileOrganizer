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

        // ✓ مسار مركزي للـ AppData (آمن، بدون admin)
        private static readonly string AppDataDir = System.IO.Path.Combine(
            Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
            "SmartFileOrganizer" );

        public App()
        {
            // ✓ Dispatcher Exceptions - كتابة آمنة
            this.DispatcherUnhandledException += ( s, e ) =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory( AppDataDir );
                    string logFile = System.IO.Path.Combine( AppDataDir, "async_crash1.txt" );
                    System.IO.File.WriteAllText( logFile, e.Exception.ToString() );
                }
                catch ( System.Exception logEx )
                {
                    // Logging failed - don't crash
                    System.Diagnostics.Debug.WriteLine( $"Failed to write crash log: {logEx.Message}" );
                }
                finally
                {
                    e.Handled = true;
                }
            };

            // ✓ AppDomain Exceptions - كتابة آمنة
            System.AppDomain.CurrentDomain.UnhandledException += ( s, e ) =>
            {
                if ( e.ExceptionObject != null )
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory( AppDataDir );
                        string logFile = System.IO.Path.Combine( AppDataDir, "async_crash2.txt" );
                        System.IO.File.WriteAllText( logFile, e.ExceptionObject.ToString() );
                    }
                    catch ( System.Exception logEx )
                    {
                        System.Diagnostics.Debug.WriteLine( $"Failed to write crash log: {logEx.Message}" );
                    }
                }
            };

            // ✓ Task Scheduler Exceptions - كتابة آمنة
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += ( s, e ) =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory( AppDataDir );
                    string logFile = System.IO.Path.Combine( AppDataDir, "async_crash3.txt" );
                    System.IO.File.WriteAllText( logFile, e.Exception.ToString() );
                }
                catch ( System.Exception logEx )
                {
                    System.Diagnostics.Debug.WriteLine( $"Failed to write crash log: {logEx.Message}" );
                }
                finally
                {
                    e.SetObserved();  // Prevent app crash
                }
            };
        }

        protected override async void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            try
            {
                System.Windows.Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

                var splashWindow = new SmartFileOrganizer.UI.Views.SplashWindow();
                splashWindow.Show();

                var serviceCollection = new ServiceCollection();

                // ✓ استخدم AppDataDir المركزي
                System.IO.Directory.CreateDirectory( AppDataDir );
                string dbPath = System.IO.Path.Combine( AppDataDir, "smartfileorganizer.db" );
                
                serviceCollection.AddDbContext<AppDbContext>( options => 
                    options.UseSqlite( $"Data Source={dbPath}" ) );
                serviceCollection.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
                serviceCollection.AddScoped<IOperationHistoryRepository, OperationHistoryRepository>();
                serviceCollection.AddScoped<IScanSessionRepository, ScanSessionRepository>();

                // Existing Services
                serviceCollection.AddSingleton<SmartFileOrganizer.Core.Interfaces.ISettingsService, 
                    SmartFileOrganizer.Infrastructure.Services.SettingsService>();
                serviceCollection.AddTransient<IFileScanner, SmartFileOrganizer.Infrastructure.FileScanner>();
                
                // New Services
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

                // Core Scanning Services (Missing from DI Review)
                serviceCollection.AddTransient<SmartFileOrganizer.Core.Interfaces.IHashService, 
                    SmartFileOrganizer.Infrastructure.HashService>();
                serviceCollection.AddTransient<SmartFileOrganizer.Application.DuplicateEngine>();
                serviceCollection.AddTransient<SmartFileOrganizer.Application.Services.ScanOrchestrator>();
                serviceCollection.AddScoped<SmartFileOrganizer.Core.Interfaces.IRepository>(sp =>
                {
                    var dbPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SmartFileOrganizer",
                        "smartfileorganizer.db");
                    return new SmartFileOrganizer.Infrastructure.SQLiteRepository(dbPath);
                });
                
                // ✓ Logging (required by DuplicateEngine, ScanOrchestrator, etc.)
                serviceCollection.AddLogging();

                // ✓ Hash Cache (persists across scans for ~90% speedup on repeat scans)
                serviceCollection.AddSingleton<SmartFileOrganizer.Core.Models.HashCacheService>();

                // ViewModels and MainWindow
                serviceCollection.AddSingleton<MainViewModel>();
                serviceCollection.AddSingleton<DashboardViewModel>();
                serviceCollection.AddSingleton<OverviewViewModel>();
                serviceCollection.AddSingleton<SettingsViewModel>();
                serviceCollection.AddTransient<MainWindow>();
                
                ServiceProvider = serviceCollection.BuildServiceProvider();

                // ✓ Apply Database Migrations safely
                await System.Threading.Tasks.Task.Run( () =>
                {
                    try
                    {
                        using ( var scope = ServiceProvider.CreateScope() )
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            dbContext.Database.Migrate();
                        }
                    }
                    catch ( System.Exception migrationEx )
                    {
                        WriteDebugLog( "Migration failed: " + migrationEx.Message );
                    }
                } );

                // ✓ Resolve MainWindow with dedicated DI crash guard
                try
                {
                    var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                    mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
                    System.Windows.Application.Current.MainWindow = mainWindow;
                    System.Windows.Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
                    
                    mainWindow.Show();
                    splashWindow.Close();
                }
                catch ( System.Exception diEx )
                {
                    splashWindow.Close();
                    string crashPath = System.IO.Path.Combine( AppDataDir, "fatal_crash.txt" );
                    System.IO.Directory.CreateDirectory( AppDataDir );
                    System.IO.File.WriteAllText( crashPath, "DI/Startup Crash: " + diEx.ToString() );
                    System.Windows.MessageBox.Show(
                        "Fatal error during startup. See localappdata for details.\n\n" + diEx.Message,
                        "Startup Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error );
                    Current.Shutdown();
                    return;
                }
                WriteDebugLog( "4. Show called" );
            }
            catch ( System.Exception ex )
            {
                try
                {
                    System.IO.Directory.CreateDirectory( AppDataDir );
                    string crashLogPath = System.IO.Path.Combine( AppDataDir, "fatal_crash.txt" );
                    System.IO.File.WriteAllText( crashLogPath, ex.ToString() );
                }
                catch ( System.Exception logEx )
                {
                    System.Diagnostics.Debug.WriteLine( $"Could not write crash log: {logEx.Message}" );
                }

                System.Windows.MessageBox.Show( 
                    ex.Message, 
                    "Fatal Startup Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error );
                
                Current.Shutdown();
            }
        }

        // ✓ دالة مساعدة آمنة للـ debug logging
        private static void WriteDebugLog( string message )
        {
            try
            {
                System.IO.Directory.CreateDirectory( AppDataDir );
                string logPath = System.IO.Path.Combine( AppDataDir, "startup_debug.txt" );
                string timestamp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss.fff" );
                System.IO.File.AppendAllText( logPath, $"[{timestamp}] {message}\n" );
            }
            catch ( System.Exception ex )
            {
                System.Diagnostics.Debug.WriteLine( $"Failed to write debug log: {ex.Message}" );
            }
        }
    }
}
