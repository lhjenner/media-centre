// Provides season and episode data for the Show Detail screen.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaPlayerApp.Application.Interfaces;
using MediaPlayerApp.Application.UseCases.LibraryScanning;
using MediaPlayerApp.Application.UseCases.Metadata;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;
using MediaPlayerApp.Domain.Entities;
using MediaPlayerApp.Domain.ValueObjects;

namespace MediaPlayerApp.UI.ViewModels.ShowDetail;

/// <summary>
/// Provides season and episode data for the Show Detail screen: loads the selected show from the
/// scanned library, resolves its poster, and allows navigating to the Player for a chosen episode.
/// </summary>
public class ShowDetailViewModel : INotifyPropertyChanged
{
    private readonly ScanLibraryUseCase _scanLibraryUseCase;
    private readonly ResolvePosterUseCase _resolvePosterUseCase;
    private readonly ILibraryRepository _libraryRepository;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    private string? _showName;
    private string? _posterPath;
    private bool _isLoading;
    private string? _errorMessage;

    public ShowDetailViewModel(
        ScanLibraryUseCase scanLibraryUseCase,
        ResolvePosterUseCase resolvePosterUseCase,
        ILibraryRepository libraryRepository,
        INavigationService navigationService,
        ILogger logger)
    {
        _scanLibraryUseCase = scanLibraryUseCase ?? throw new ArgumentNullException(nameof(scanLibraryUseCase));
        _resolvePosterUseCase = resolvePosterUseCase ?? throw new ArgumentNullException(nameof(resolvePosterUseCase));
        _libraryRepository = libraryRepository ?? throw new ArgumentNullException(nameof(libraryRepository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        SelectEpisodeCommand = new RelayCommand<Episode>(SelectEpisode);
        BackCommand = new RelayCommand(() => _navigationService.NavigateToHome());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Season> Seasons { get; } = new();

    public string? ShowName
    {
        get => _showName;
        private set => SetProperty(ref _showName, value);
    }

    public string? PosterPath
    {
        get => _posterPath;
        private set => SetProperty(ref _posterPath, value);
    }

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

    public ICommand SelectEpisodeCommand { get; }

    public ICommand BackCommand { get; }

    /// <summary>
    /// Loads the show at the given folder path, populating <see cref="Seasons"/> and <see cref="PosterPath"/>.
    /// </summary>
    public async Task LoadShowAsync(string showFolderPath)
    {
        if (string.IsNullOrWhiteSpace(showFolderPath))
        {
            throw new ArgumentException("Show folder path must be provided.", nameof(showFolderPath));
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.Info($"Loading Show Detail for '{showFolderPath}'.");

            var show = await Task.Run(() =>
            {
                var library = _libraryRepository.GetLibrary();
                var found = library.FirstOrDefault(s => string.Equals(s.FolderPath, showFolderPath, StringComparison.OrdinalIgnoreCase));

                if (found is not null)
                {
                    return found;
                }

                var rescannedLibrary = _scanLibraryUseCase.Execute();
                return rescannedLibrary.FirstOrDefault(s => string.Equals(s.FolderPath, showFolderPath, StringComparison.OrdinalIgnoreCase));
            });

            if (show is null)
            {
                _logger.Warning($"Show not found for folder path '{showFolderPath}'.");
                ErrorMessage = "Show could not be found.";
                return;
            }

            ShowName = show.Name;

            Seasons.Clear();
            foreach (var season in show.Seasons)
            {
                Seasons.Add(season);
            }

            PosterPath = await _resolvePosterUseCase.ExecuteForShowAsync(show.Name, show.FolderPath);

            _logger.Info($"Loaded {Seasons.Count} season(s) for show '{show.Name}'.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load Show Detail for '{showFolderPath}'.", ex);
            ErrorMessage = "Unable to load this show. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectEpisode(Episode? episode)
    {
        if (episode is null)
        {
            return;
        }

        _logger.Info($"Navigating to Player for episode '{episode.Key}'.");
        _navigationService.NavigateToPlayer(episode.Key.ToString());
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
