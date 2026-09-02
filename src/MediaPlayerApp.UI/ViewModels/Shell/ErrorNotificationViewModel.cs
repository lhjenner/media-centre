// Holds and exposes the current user-facing error/notification banner state.
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MediaPlayerApp.Common.Logging;

namespace MediaPlayerApp.UI.ViewModels.Shell;

/// <summary>
/// Holds and exposes the current user-facing error/notification banner state for the shell,
/// allowing any screen view-model to surface a transient error message to the user.
/// </summary>
public class ErrorNotificationViewModel : INotifyPropertyChanged
{
    private readonly ILogger _logger;

    private string? _message;
    private bool _isVisible;

    public ErrorNotificationViewModel(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        DismissCommand = new RelayCommand(Dismiss);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public ICommand DismissCommand { get; }

    /// <summary>
    /// Displays the given message in the notification banner.
    /// </summary>
    public void ShowError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _logger.Warning($"Displaying error notification: {message}");

        Message = message;
        IsVisible = true;
    }

    /// <summary>
    /// Logs the given exception and displays the given user-facing message in the notification banner.
    /// </summary>
    public void ShowError(string message, Exception exception)
    {
        _logger.Error(message, exception);

        Message = message;
        IsVisible = true;
    }

    /// <summary>
    /// Clears and hides the notification banner.
    /// </summary>
    public void Dismiss()
    {
        Message = null;
        IsVisible = false;
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
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
