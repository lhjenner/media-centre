// Manages shell-level state and navigation.
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaPlayerApp.Application.Interfaces;
using MediaPlayerApp.Common.Logging;

namespace MediaPlayerApp.UI.ViewModels.Shell;

/// <summary>
/// Manages shell-level state for the main window: tracks the currently displayed screen
/// view-model and exposes shell-wide navigation (e.g., returning Home) and error state.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    private object? _currentViewModel;
    private string? _errorMessage;

    public MainWindowViewModel(
        INavigationService navigationService,
        ILogger logger)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        NavigateHomeCommand = new RelayCommand(async () => await NavigateHomeAsync());
        DismissErrorCommand = new RelayCommand(() => ErrorMessage = null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the view-model for the screen currently displayed in the shell's content area.
    /// </summary>
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand NavigateHomeCommand { get; }

    public ICommand DismissErrorCommand { get; }

    /// <summary>
    /// Navigates the shell back to the Home screen.
    /// </summary>
    public async Task NavigateHomeAsync()
    {
        ErrorMessage = null;

        try
        {
            _logger.Info("Navigating to Home from shell.");
            await Task.Run(() => _navigationService.NavigateToHome());
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to navigate to Home.", ex);
            ErrorMessage = "Unable to navigate to Home. Please try again.";
        }
    }

    /// <summary>
    /// Displays a shell-wide error message.
    /// </summary>
    public void ShowError(string message)
    {
        _logger.Warning($"Displaying shell error: {message}");
        ErrorMessage = message;
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _execute;

        public RelayCommand(Func<Task> executeAsync)
        {
            _executeAsync = executeAsync;
        }

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter)
        {
            if (_executeAsync is not null)
            {
                await _executeAsync();
            }
            else
            {
                _execute?.Invoke();
            }
        }
    }
}
