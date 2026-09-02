// Provides the Continue Watching items for display.
// Provides the Continue Watching items for display.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaPlayerApp.Application.DTOs;
using MediaPlayerApp.Application.Interfaces;
using MediaPlayerApp.Application.UseCases.Playback;
using MediaPlayerApp.Application.UseCases.Progress;
using MediaPlayerApp.Common.Logging;

namespace MediaPlayerApp.UI.ViewModels.Home;

/// <summary>
/// Provides the Continue Watching row for the Home screen: loads in-progress episodes and
/// allows resuming playback for a selected item.
/// </summary>
public class ContinueWatchingViewModel : INotifyPropertyChanged
{
    private readonly GetContinueWatchingUseCase _getContinueWatchingUseCase;
    private readonly ResumePlaybackUseCase _resumePlaybackUseCase;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    private bool _isLoading;
    private string? _errorMessage;

    public ContinueWatchingViewModel(
        GetContinueWatchingUseCase getContinueWatchingUseCase,
        ResumePlaybackUseCase resumePlaybackUseCase,
        INavigationService navigationService,
        ILogger logger)
    {
        _getContinueWatchingUseCase = getContinueWatchingUseCase ?? throw new ArgumentNullException(nameof(getContinueWatchingUseCase));
        _resumePlaybackUseCase = resumePlaybackUseCase ?? throw new ArgumentNullException(nameof(resumePlaybackUseCase));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadCommand = new RelayCommand(async () => await LoadAsync());
        ResumeCommand = new RelayCommand<ContinueWatchingItemDto>(Resume);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ContinueWatchingItemDto> Items { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand LoadCommand { get; }

    public ICommand ResumeCommand { get; }

    /// <summary>
    /// Loads the Continue Watching list and populates <see cref="Items"/>.
    /// </summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.Info("Loading Continue Watching items for Home screen.");

            var items = await Task.Run(() => _getContinueWatchingUseCase.Execute());

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            _logger.Info($"Loaded {Items.Count} Continue Watching item(s).");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load Continue Watching items.", ex);
            ErrorMessage = "Unable to load Continue Watching. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Resume(ContinueWatchingItemDto? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            _logger.Info($"Resuming playback for '{item.EpisodeKey}'.");
            _resumePlaybackUseCase.Execute(item.EpisodeKey);
            _navigationService.NavigateToPlayer(item.EpisodeKey.ToString());
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to resume playback for '{item.EpisodeKey}'.", ex);
            ErrorMessage = "Unable to resume playback. Please try again.";
        }
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
        private readonly Func<Task> _executeAsync;

        public RelayCommand(Func<Task> executeAsync)
        {
            _executeAsync = executeAsync;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter) => await _executeAsync();
    }

    private sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;

        public RelayCommand(Action<T?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter is T typed ? typed : default);
    }
}
