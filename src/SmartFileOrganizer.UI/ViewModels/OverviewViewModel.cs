using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.UI.Mvvm;

namespace SmartFileOrganizer.UI.ViewModels
{
    public class OverviewViewModel : ViewModelBase
    {
        private readonly ITempCleanupService _tempCleanupService;
        private readonly IDriveRelocationService _driveRelocationService;
        private readonly IRepository _repository;
        private readonly System.Collections.Generic.List<OverviewItemModel> _activeSourceFiles = new();

        public class OverviewItemModel
        {
            public string FileName { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }

        public OverviewViewModel( ITempCleanupService tempCleanupService, IDriveRelocationService driveRelocationService, IRepository repository )
        {
            _tempCleanupService = tempCleanupService;
            _driveRelocationService = driveRelocationService;
            _repository = repository;

            CleanTempCommand = new RelayCommand( async _ => await ExecuteCleanTempAsync() );
            BrowseSourceCommand = new RelayCommand( _ => ExecuteBrowseSource() );
            BrowseDestinationCommand = new RelayCommand( _ => ExecuteBrowseDestination() );
            RelocateDriveCommand = new RelayCommand( async _ => await ExecuteRelocateDriveAsync() );
            OpenScannedFilesCommand = new RelayCommand( _ => ExecuteOpenScannedFiles() );
            OpenDuplicatesCommand = new RelayCommand( _ => ExecuteOpenDuplicates() );
            BackToCardsCommand = new RelayCommand( _ => ExecuteBackToCards() );
            FilterCommand = new RelayCommand( param => ExecuteFilter( param as string ) );

            SmartFileOrganizer.Core.Events.GlobalEvents.OnLanguageChanged += () =>
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    TempCleanupResult = string.Empty;
                }));
            };
        }

        // --- Summary Cards ---
        private int _totalScannedFiles;
        public int TotalScannedFiles
        {
            get => _totalScannedFiles;
            set => SetProperty( ref _totalScannedFiles, value );
        }

        private int _duplicatesFound;
        public int DuplicatesFound
        {
            get => _duplicatesFound;
            set => SetProperty( ref _duplicatesFound, value );
        }

        // --- State Visibility ---
        private System.Windows.Visibility _cardsViewVisibility = System.Windows.Visibility.Visible;
        public System.Windows.Visibility CardsViewVisibility
        {
            get => _cardsViewVisibility;
            set => SetProperty( ref _cardsViewVisibility, value );
        }

        private System.Windows.Visibility _detailViewVisibility = System.Windows.Visibility.Collapsed;
        public System.Windows.Visibility DetailViewVisibility
        {
            get => _detailViewVisibility;
            set => SetProperty( ref _detailViewVisibility, value );
        }

        private string _detailTitleText = "Category Details";
        public string DetailTitleText
        {
            get => _detailTitleText;
            set => SetProperty( ref _detailTitleText, value );
        }

        public ICommand OpenScannedFilesCommand { get; }
        public ICommand OpenDuplicatesCommand { get; }
        public ICommand BackToCardsCommand { get; }

        public ICommand FilterCommand { get; }

        private void ExecuteOpenScannedFiles()
        {
            DetailTitleText = global::System.Windows.Application.Current.TryFindResource("StrTitleTotalScannedFiles") as string ?? "Total Scanned Files";
            CardsViewVisibility = System.Windows.Visibility.Collapsed;
            DetailViewVisibility = System.Windows.Visibility.Visible;
            
            _activeSourceFiles.Clear();
            foreach ( var file in SmartFileOrganizer.UI.ViewModels.ScanSessionStore.AllScannedFiles )
            {
                string safePath = file.Path ?? string.Empty;
                string safeName = System.IO.Path.GetFileName( safePath );
                _activeSourceFiles.Add( new OverviewItemModel
                {
                    FileName = safeName,
                    Path = safePath,
                    Category = SmartFileOrganizer.UI.ViewModels.ScanSessionStore.GetCategory( safeName )
                } );
            }
            ExecuteFilter( "All" );
        }

        private void ExecuteOpenDuplicates()
        {
            DetailTitleText = global::System.Windows.Application.Current.TryFindResource("StrTitleDuplicatesFound") as string ?? "Duplicates Found";
            CardsViewVisibility = System.Windows.Visibility.Collapsed;
            DetailViewVisibility = System.Windows.Visibility.Visible;
            
            _activeSourceFiles.Clear();
            foreach ( var item in SmartFileOrganizer.UI.ViewModels.ScanSessionStore.AllDuplicates )
            {
                _activeSourceFiles.Add( new OverviewItemModel
                {
                    FileName = item.FileName,
                    Path = item.Path,
                    Category = SmartFileOrganizer.UI.ViewModels.ScanSessionStore.GetCategory( item.FileName )
                } );
            }
            ExecuteFilter( "All" );
        }

        private void ExecuteFilter( string? category )
        {
            if ( string.IsNullOrWhiteSpace( category ) ) category = "All";

            FilteredFiles.Clear();
            var results = category == "All" 
                ? _activeSourceFiles 
                : _activeSourceFiles.Where( x => x.Category == category );
                
            foreach ( var item in results )
            {
                FilteredFiles.Add( item );
            }
        }

        private void ExecuteBackToCards()
        {
            CardsViewVisibility = System.Windows.Visibility.Visible;
            DetailViewVisibility = System.Windows.Visibility.Collapsed;
        }

        public void UpdateCards()
        {
            _ = LoadStatisticsAsync();
        }

        public async Task LoadStatisticsAsync()
        {
            try
            {
                var sessions = await _repository.GetScanSessionsAsync();
                var groups = await _repository.GetDuplicateGroupsAsync();
                TotalScannedFiles = sessions.Sum(s => s.FilesDiscovered);
                DuplicatesFound = groups.Sum(g => g.Files.Count);
            }
            catch
            {
                TotalScannedFiles = 0;
                DuplicatesFound = 0;
            }
        }

        public System.Collections.ObjectModel.ObservableCollection<object> FilteredFiles { get; } = new System.Collections.ObjectModel.ObservableCollection<object>();

        // Ensure we can notify when summary changes (call this from scanner or when navigating)
        public async Task RefreshSummary()
        {
            await LoadStatisticsAsync();
        }

        // --- Temp Cleanup ---
        private string _tempCleanupResult = string.Empty;
        public string TempCleanupResult
        {
            get => _tempCleanupResult;
            set { _tempCleanupResult = value; OnPropertyChanged(); }
        }
        public ICommand CleanTempCommand { get; }

        private async Task ExecuteCleanTempAsync()
        {
            try
            {
                TempCleanupResult = "Cleaning temp files...";
                var summary = await _tempCleanupService.CleanAsync();
                var format = global::System.Windows.Application.Current.Resources["TempCleanupResult"] as string ?? "Deleted {0} files ({1} bytes). Skipped {2} files.";
                TempCleanupResult = string.Format(format, summary.FilesDeleted, summary.BytesDeleted, summary.FilesSkipped);
            }
            catch ( Exception ex )
            {
                TempCleanupResult = $"Error: { ex.Message }";
            }
        }

        // --- Drive Relocation ---
        private string _sourcePath = string.Empty;
        public string SourcePath
        {
            get => _sourcePath;
            set => SetProperty( ref _sourcePath, value );
        }

        private string _destinationPath = string.Empty;
        public string DestinationPath
        {
            get => _destinationPath;
            set => SetProperty( ref _destinationPath, value );
        }

        private string _relocationResultText = string.Empty;
        public string RelocationResultText
        {
            get => _relocationResultText;
            set => SetProperty( ref _relocationResultText, value );
        }

        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseDestinationCommand { get; }
        public ICommand RelocateDriveCommand { get; }

        private void ExecuteBrowseSource()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if ( dialog.ShowDialog() == true )
            {
                SourcePath = dialog.FolderName;
            }
        }

        private void ExecuteBrowseDestination()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if ( dialog.ShowDialog() == true )
            {
                DestinationPath = dialog.FolderName;
            }
        }

        private async Task ExecuteRelocateDriveAsync()
        {
            try
            {
                if ( string.IsNullOrWhiteSpace( SourcePath ) || string.IsNullOrWhiteSpace( DestinationPath ) )
                {
                    RelocationResultText = "Error: Source and destination paths are required.";
                    return;
                }

                RelocationResultText = "Relocating files...";
                var result = await _driveRelocationService.RelocateAsync( SourcePath, DestinationPath );

                if ( result.IsSuccess )
                {
                    var msgTemplate = global::System.Windows.Application.Current.TryFindResource("StrMsgRelocationResult") as string ?? "Relocated {0} files ({1} bytes). Skipped {2} files.";
                    RelocationResultText = string.Format(msgTemplate, result.FilesProcessed, result.BytesProcessed, result.FilesSkipped);
                }
                else
                {
                    RelocationResultText = $"Relocation failed: { result.ErrorMessage }";
                }
            }
            catch ( Exception ex )
            {
                RelocationResultText = $"Error: { ex.Message }";
            }
        }
    }
}
