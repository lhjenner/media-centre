// Manages playback state and controls for the Player screen.
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaPlayerApp.Application.Interfaces;
using MediaPlayerApp.Application.UseCases.Playback;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.UI.ViewModels.Player;

/// <summary>
/// Manages playback state and controls for the Player screen: starts/resumes playback for a
/// selected episode and exposes play/pause/stop/seek controls backed by the playback engine.
/// </summary>
public class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly PlayEpisodeUseCase _playEpisodeUseCase;
    private readonly ResumePlaybackUseCase _resumePlaybackUseCase;
    private readonly IMediaPlaybackEngine _playbackEngine;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    private bool _isPlaying;
    private TimeSpan _position;
    private TimeSpan _duration;
    private string? _errorMessage;

    public PlayerViewModel(
        PlayEpisodeUseCase playEpisodeUseCase,
        ResumePlaybackUseCase resumePlaybackUseCase,
        IMediaPlaybackEngine playbackEngine,
        INavigationService navigationService,
        ILogger logger)
    {
        _playEpisodeUseCase = playEpisodeUseCase ?? throw new ArgumentNullException(nameof(playEpisodeUseCase));
        _resumePlaybackUseCase = resumePlaybackUseCase ?? throw new ArgumentNullException(nameof(resumePlaybackUseCase));
        _playbackEngine = playbackEngine ?? throw new ArgumentNullException(nameof(playbackEngine));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _playbackEngine.TimeChanged += OnTimeChanged;
        _playbackEngine.Paused += OnPlaybackStateChanged;
        _playbackEngine.Stopped += OnPlaybackStateChanged;
        _playbackEngine.EndReached += OnPlaybackStateChanged;

        PlayCommand = new RelayCommand(Play);
        PauseCommand = new RelayCommand(Pause);
        StopCommand = new RelayCommand(Stop);
        SeekCommand = new RelayCommand<TimeSpan>(SeekTo);
        BackCommand = new RelayCommand(GoBack);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetProperty(ref _isPlaying, value);
    }

    public TimeSpan Position
    {
        get => _position;
        private set => SetProperty(ref _position, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        private set => SetProperty(ref _duration, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ICommand PlayCommand { get; }

    public ICommand PauseCommand { get; }

    public ICommand StopCommand { get; }

    public ICommand SeekCommand { get; }

    public ICommand BackCommand { get; }

    /// <summary>
    /// Loads and starts playback of the episode matching the given key.
    /// </summary>
    public Task LoadEpisodeAsync(EpisodeKey episodeKey)
    {
        return RunPlaybackActionAsync(
            () => _playEpisodeUseCase.Execute(episodeKey),
            $"Failed to start playback for episode '{episodeKey}'.");
    }

    /// <summary>
    /// Resumes playback of the episode matching the given key from its stored progress position.
    /// </summary>
    public Task ResumeEpisodeAsync(EpisodeKey episodeKey)
    {
        return RunPlaybackActionAsync(
            () => _resumePlaybackUseCase.Execute(episodeKey),
            $"Failed to resume playback for episode '{episodeKey}'.");
    }

    private async Task RunPlaybackActionAsync(Action action, string errorMessage)
    {
        ErrorMessage = null;

        try
        {
            await Task.Run(action);
            IsPlaying = _playbackEngine.IsPlaying;
        }
        catch (Exception ex)
        {
            _logger.Error(errorMessage, ex);
            ErrorMessage = "Unable to start playback. Please try again.";
        }
    }

    private void Play()
    {
        try
        {
            _playbackEngine.Play();
            IsPlaying = _playbackEngine.IsPlaying;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to resume playback.", ex);
            ErrorMessage = "Unable to resume playback. Please try again.";
        }
    }

    private void Pause()
    {
        try
        {
            _playbackEngine.Pause();
            IsPlaying = _playbackEngine.IsPlaying;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to pause playback.", ex);
            ErrorMessage = "Unable to pause playback. Please try again.";
        }
    }

    private void Stop()
    {
        try
        {
            _playbackEngine.Stop();
            IsPlaying = _playbackEngine.IsPlaying;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to stop playback.", ex);
            ErrorMessage = "Unable to stop playback. Please try again.";
        }
    }

    private void SeekTo(TimeSpan position)
    {
        try
        {
            _playbackEngine.SeekTo(position);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to seek to '{position}'.", ex);
            ErrorMessage = "Unable to seek. Please try again.";
        }
    }

    private void GoBack()
    {
        _logger.Info("Navigating back to Home from Player.");
        Stop();
        _navigationService.NavigateToHome();
    }

    private void OnTimeChanged(object? sender, PlaybackPosition position)
    {
        Position = position.Position;
        Duration = position.Duration;
    }

    private void OnPlaybackStateChanged(object? sender, EventArgs e)
    {
        IsPlaying = _playbackEngine.IsPlaying;
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

    public void Dispose()
    {
        _playbackEngine.TimeChanged -= OnTimeChanged;
        _playbackEngine.Paused -= OnPlaybackStateChanged;
        _playbackEngine.Stopped -= OnPlaybackStateChanged;
        _playbackEngine.EndReached -= OnPlaybackStateChanged;
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

    private sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;

        public RelayCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (parameter is T typed)
            {
                _execute(typed);
            }
        }
    }
}
