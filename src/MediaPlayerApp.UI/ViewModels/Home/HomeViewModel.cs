// Provides the shows grid data for the Home screen.
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MediaPlayerApp.Application.DTOs;
using MediaPlayerApp.Application.Interfaces;
using MediaPlayerApp.Application.UseCases.LibraryScanning;
using MediaPlayerApp.Common.Logging;
using MediaPlayerApp.Domain.Abstractions;

namespace MediaPlayerApp.UI.ViewModels.Home;

/// <summary>
/// Provides the shows grid data for the Home screen: triggers the initial library scan and
/// exposes the resulting shows for display, along with navigation to the Show Detail screen.
/// </summary>
public class HomeViewModel : INotifyPropertyChanged
{
    private readonly ScanLibraryUseCase _scanLibraryUseCase;
    private readonly IMetadataProvider _localPosterProvider;
    private readonly INavigationService _navigationService;
    private readonly ILogger _logger;

    private bool _isLoading;
    private string? _errorMessage;

    public HomeViewModel(
        ScanLibraryUseCase scanLibraryUseCase,
        IMetadataProvider localPosterProvider,
        INavigationService navigationService,
        ILogger logger)
    {
        _scanLibraryUseCase = scanLibraryUseCase ?? throw new ArgumentNullException(nameof(scanLibraryUseCase));
        _localPosterProvider = localPosterProvider ?? throw new ArgumentNullException(nameof(localPosterProvider));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadShowsCommand = new RelayCommand(async () => await LoadShowsAsync());
        SelectShowCommand = new RelayCommand<ShowSummaryDto>(SelectShow);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ShowSummaryDto> Shows { get; } = new();

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

    public ICommand LoadShowsCommand { get; }

    public ICommand SelectShowCommand { get; }

    /// <summary>
    /// Scans the Videos root folder and populates <see cref="Shows"/>.
    /// </summary>
    public async Task LoadShowsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.Info("Loading shows for Home screen.");

            var shows = await Task.Run(() => _scanLibraryUseCase.Execute());

            Shows.Clear();
            foreach (var showDto in shows.Select(ToDto))
            {
                Shows.Add(showDto);
            }

            _logger.Info($"Loaded {Shows.Count} show(s) for Home screen.");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load shows for Home screen.", ex);
            ErrorMessage = "Unable to load your library. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectShow(ShowSummaryDto? show)
    {
        if (show is null)
        {
            return;
        }

        _logger.Info($"Navigating to Show Detail for '{show.Name}'.");
        _navigationService.NavigateToShowDetail(show.FolderPath);
    }

    private ShowSummaryDto ToDto(Domain.Entities.Show show)
    {
        var posterPath = _localPosterProvider.GetShowPosterPath(show.FolderPath);
        return new ShowSummaryDto(show.Name, show.FolderPath, posterPath);
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

