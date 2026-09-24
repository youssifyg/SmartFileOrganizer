using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SmartFileOrganizer.Core.Interfaces;
using SmartFileOrganizer.Core.Models;
using MessageBox = System.Windows.MessageBox;
using SmartFileOrganizer.UI.Mvvm;

namespace SmartFileOrganizer.UI.ViewModels
{
    public class DuplicateItem : ViewModelBase
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty( ref _isSelected, value );
        }
        public string GroupId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public static class ScanSessionStore
    {
        public static List<FileRecord> AllScannedFiles { get; set; } = new();
        public static List<DuplicateItem> AllDuplicates { get; set; } = new();

        public static int TotalScannedFiles => AllScannedFiles.Count;
        public static int TotalDuplicatesFound => AllDuplicates.Count;

        public static string GetCategory( string fileName )
        {
            string ext = System.IO.Path.GetExtension( fileName ).ToLowerInvariant();
            if ( new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains( ext ) ) return "Images";
            if ( new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".txt", ".pptx", ".csv" }.Contains( ext ) ) return "Documents";
            if ( new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv" }.Contains( ext ) ) return "Videos";
            if ( new[] { ".mp3", ".wav", ".aac", ".flac", ".m4a" }.Contains( ext ) ) return "Sounds";
            return "Other";
        }
    }

    public class DashboardViewModel : ViewModelBase
    {
        private readonly IFileScanner _fileScanner;
        private readonly SmartFileOrganizer.Application.Services.ScanOrchestrator _scanOrchestrator;

        public DashboardViewModel( IFileScanner fileScanner, SmartFileOrganizer.Application.Services.ScanOrchestrator scanOrchestrator )
        {
            _fileScanner = fileScanner;
            _scanOrchestrator = scanOrchestrator;
            DuplicateFiles = new ObservableCollection<DuplicateItem>();

            BrowseScanCommand = new RelayCommand( _ => ExecuteBrowseScan() );
            StartScanCommand = new RelayCommand( async _ => await ExecuteStartScanAsync() );
            ResetScanCommand = new RelayCommand( _ => ExecuteResetScan() );
            OpenLocationCommand = new RelayCommand( _ => ExecuteOpenLocation() );
            RecycleBinCommand = new RelayCommand( _ => ExecuteRecycleBin() );
        }

        public ObservableCollection<DuplicateItem> DuplicateFiles { get; }

        private string _targetPath = string.Empty;
        public string TargetPath
        {
            get => _targetPath;
            set => SetProperty( ref _targetPath, value );
        }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty( ref _isScanning, value );
        }

        private string _progressText = string.Empty;
        public string ProgressText
        {
            get => _progressText;
            set => SetProperty( ref _progressText, value );
        }

        private string _selectedCountText = "0 items";
        public string SelectedCountText
        {
            get => _selectedCountText;
            set => SetProperty( ref _selectedCountText, value );
        }

        private void UpdateSelectedCount()
        {
            int count = DuplicateFiles.Count( x => x.IsSelected );
            var template = global::System.Windows.Application.Current.TryFindResource("StrLabelSelectedItemsCount") as string ?? "{0} items";
            SelectedCountText = string.Format( template, count );
        }

        public ICommand BrowseScanCommand { get; }
        public ICommand StartScanCommand { get; }
        public ICommand ResetScanCommand { get; }
        public ICommand OpenLocationCommand { get; }
        public ICommand RecycleBinCommand { get; }

        private void ExecuteBrowseScan()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            if ( dialog.ShowDialog() == true )
            {
                TargetPath = dialog.FolderName;
            }
        }

        private async Task ExecuteStartScanAsync()
        {
            if ( string.IsNullOrWhiteSpace( TargetPath ) )
            {
                MessageBox.Show( "Please select a folder first." );
                return;
            }

            DuplicateFiles.Clear();
            UpdateSelectedCount();
            ScanSessionStore.AllScannedFiles.Clear();
            ScanSessionStore.AllDuplicates.Clear();
            IsScanning = true;
            ProgressText = "Preparing to scan...";

            try
            {
                int duplicateCount = 0;
                var progress = new Progress<SmartFileOrganizer.Core.Models.ScanProgressReport>(report =>
                {
                    ProgressText = $"{report.Stage}... {report.FilesAnalyzed}/{report.FilesDiscovered} - {System.IO.Path.GetFileName(report.CurrentFile)}";
                });

                await Task.Run(async () =>
                {
                    int groupId = 1;
                    
                    await foreach (var group in _scanOrchestrator.RunAsync(TargetPath, progress))
                    {
                        var batch = new List<DuplicateItem>();
                        foreach (var file in group.Files)
                        {
                            string safePath = file.Path ?? string.Empty;
                            var item = new DuplicateItem
                            {
                                GroupId = groupId.ToString(),
                                FileName = System.IO.Path.GetFileName(safePath),
                                Path = safePath,
                                Size = $"{((long?)file.Size).GetValueOrDefault() / 1024.0 / 1024.0:F2} MB",
                                SizeBytes = ((long?)file.Size).GetValueOrDefault()
                            };
                            item.PropertyChanged += (s, e) => {
                                if (e.PropertyName == nameof(DuplicateItem.IsSelected))
                                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => UpdateSelectedCount());
                            };
                            batch.Add(item);
                        }
                        groupId++;

                        // Progressively update UI on the Dispatcher thread
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            foreach (var item in batch)
                            {
                                DuplicateFiles.Add(item);
                            }
                            UpdateSelectedCount();
                            duplicateCount += batch.Count;
                        });
                    }
                });

                ScanSessionStore.AllDuplicates = DuplicateFiles.ToList();
                UpdateSelectedCount();
                var title = global::System.Windows.Application.Current.TryFindResource("StrMsgScanCompleteTitle") as string ?? "Scan Complete";
                var bodyTemplate = global::System.Windows.Application.Current.TryFindResource("StrMsgScanCompleteBody") as string ?? "Scan completed successfully!\nFound {0} duplicate(s).";
                MessageBox.Show( string.Format(bodyTemplate, duplicateCount), title, MessageBoxButton.OK, MessageBoxImage.Information );
            }
            catch (Exception ex)
            {
                string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartFileOrganizer", "scan_error.txt");
                System.IO.File.WriteAllText(logPath, ex.ToString());
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"Scan failed! Full trace written to: {logPath}\n\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                });
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void ExecuteResetScan()
        {
            DuplicateFiles.Clear();
            UpdateSelectedCount();
            TargetPath = string.Empty;
            ScanSessionStore.AllScannedFiles.Clear();
            ScanSessionStore.AllDuplicates.Clear();
            ProgressText = string.Empty;
        }

        private void ExecuteOpenLocation()
        {
            foreach ( var item in DuplicateFiles )
            {
                if ( item.IsSelected && !string.IsNullOrEmpty( item.Path ) && File.Exists( item.Path ) )
                {
                    try
                    {
                        Process.Start( new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{ item.Path }\"",
                            UseShellExecute = true
                        } );
                    }
                    catch { }
                }
            }
        }

        private void ExecuteRecycleBin()
        {
            var selectedItems = DuplicateFiles.Where( x => x.IsSelected ).ToList();
            if ( selectedItems.Count == 0 )
            {
                var titleReq = global::System.Windows.Application.Current.TryFindResource("StrMsgSelectionRequiredTitle") as string ?? "Selection Required";
                var bodyReq = global::System.Windows.Application.Current.TryFindResource("StrMsgSelectionRequiredBody") as string ?? "Please select at least one file to move to the Recycle Bin.";
                MessageBox.Show( bodyReq, titleReq, MessageBoxButton.OK, MessageBoxImage.Warning );
                return;
            }

            var msgBodyTemplate = global::System.Windows.Application.Current.TryFindResource("StrMsgConfirmDeleteBody") as string ?? "Are you sure you want to move {0} file(s) to the Recycle Bin?";
            var msgTitle = global::System.Windows.Application.Current.TryFindResource("StrMsgConfirmDeleteTitle") as string ?? "Confirm Safe Deletion";
            var msgBody = string.Format(msgBodyTemplate, selectedItems.Count);
            
            var result = MessageBox.Show( msgBody, msgTitle, MessageBoxButton.YesNo, MessageBoxImage.Question );
            if ( result == MessageBoxResult.Yes )
            {
                foreach ( var item in selectedItems )
                {
                    try
                    {
                        if ( !string.IsNullOrEmpty( item.Path ) && File.Exists( item.Path ) )
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                                item.Path,
                                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin );
                            DuplicateFiles.Remove( item );
                        }
                    }
                    catch ( Exception ex )
                    {
                        MessageBox.Show( $"Failed to delete { item.FileName }: { ex.Message }", "Error", MessageBoxButton.OK, MessageBoxImage.Error );
                    }
                }
                ScanSessionStore.AllDuplicates = DuplicateFiles.ToList();
                MessageBox.Show( "Selected files successfully moved to the Recycle Bin.", "Completed", MessageBoxButton.OK, MessageBoxImage.Information );
            }
        }
    }
}
