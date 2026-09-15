using System;
using SmartFileOrganizer.UI.Mvvm;

namespace SmartFileOrganizer.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        
        private object? _currentViewModel;
        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                SetProperty( ref _currentViewModel, value );
                OnPropertyChanged( nameof( CurrentViewModel ) );
            }
        }

        public MainViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
