# Class List — Local Netflix-Style Media Player

## MediaPlayerApp.Domain

### Entities
- **Show** — Represents a show with its name, folder path, and collection of seasons.
- **Season** — Represents a season number and its collection of episodes.
- **Episode** — Represents a single episode's file path, number, title, and metadata.
- **WatchProgress** — Represents an episode's playback position, duration, and watch status.

### Value Objects
- **EpisodeKey** — Stable identifier derived from show, season, and episode number.
- **SeasonEpisodeNumber** — Represents a parsed season/episode number pair.
- **PlaybackPosition** — Represents a playback position/duration pair with helper comparisons.

### Enums
- **WatchStatus** — Enumerates NotStarted, InProgress, and Completed states.

### Abstractions
- **ILibraryRepository** — Defines the contract for retrieving the scanned show/season/episode library.
- **IProgressRepository** — Defines the contract for loading and saving watch progress data.
- **IMetadataProvider** — Defines the contract for resolving posters and show/episode metadata.
- **IThumbnailGenerator** — Defines the contract for generating episode thumbnail images.
- **IMediaPlaybackEngine** — Defines the contract for controlling media playback.

---

## MediaPlayerApp.Application

### Library Scanning
- **ScanLibraryUseCase** — Orchestrates scanning the root Videos folder into a show/season/episode library.
- **FileNameParsingService** — Extracts show name, season, and episode numbers from folder and file names.

### Metadata
- **ResolvePosterUseCase** — Determines the poster image to use for a show or season.
- **FetchOnlineMetadataUseCase** — Retrieves and caches metadata from an online provider when needed.

### Thumbnails
- **GenerateEpisodeThumbnailUseCase** — Coordinates generating and caching a thumbnail for an episode.

### Playback
- **PlayEpisodeUseCase** — Starts playback of a selected episode.
- **ResumePlaybackUseCase** — Resumes playback of an episode from its stored progress position.

### Progress
- **UpdateProgressUseCase** — Updates and persists an episode's watch progress.
- **GetContinueWatchingUseCase** — Builds the ordered Continue Watching list from stored progress.

### DTOs
- **ShowSummaryDto** — Lightweight show data shape for UI binding.
- **EpisodeSummaryDto** — Lightweight episode data shape for UI binding.
- **ContinueWatchingItemDto** — Lightweight data shape representing a Continue Watching entry.

---

## MediaPlayerApp.Infrastructure

### File System
- **LibraryRepository** — Scans the filesystem and builds the show/season/episode library.
- **VideoFileExtensions** — Identifies supported video file extensions.
- **FolderNameCleaner** — Cleans raw folder/file names into display-friendly titles.

### Playback
- **LibVlcPlaybackEngine** — Wraps LibVLCSharp to provide play, pause, stop, and seek behavior.
- **LibVlcInitializer** — Initializes and holds the shared LibVLC engine instance.

### Thumbnails
- **VlcSnapshotThumbnailGenerator** — Generates episode thumbnails using LibVLCSharp frame snapshots.

### Metadata
- **LocalPosterProvider** — Resolves poster images from local show/season folders.
- **TmdbMetadataProvider** — Fetches show and episode metadata from TMDB.
- **MetadataCacheStore** — Caches downloaded metadata and assets on disk.

### Persistence
- **JsonProgressRepository** — Reads and writes playback progress to a JSON file.
- **AtomicFileWriter** — Writes files safely using a temp-file-then-rename pattern.

### Configuration
- **AppPaths** — Resolves application data, cache, and thumbnail folder paths.

---

## MediaPlayerApp.UI

### Views
- **MainWindow** — Hosts the application shell and navigation frame.
- **HomeView** — Displays the Continue Watching row and the main shows grid.
- **ContinueWatchingRowView** — Displays the horizontally scrolling Continue Watching row.
- **ShowDetailView** — Displays a show's poster, season selector, and episode list.
- **PlayerView** — Displays the video surface and playback controls.

### ViewModels
- **MainWindowViewModel** — Manages shell-level state and navigation.
- **HomeViewModel** — Provides the shows grid data for the Home screen.
- **ContinueWatchingViewModel** — Provides the Continue Watching items for display.
- **ShowDetailViewModel** — Provides season and episode data for the Show Detail screen.
- **PlayerViewModel** — Manages playback state and controls for the Player screen.

### Controls
- **MediaCardControl** — Reusable card displaying a poster/thumbnail, title, and progress bar.
- **PlaybackControlsBar** — Reusable playback transport controls bar.

### Navigation
- **NavigationService** — Handles navigation between application screens.

---

## MediaPlayerApp.Common

- **ILogger** — Defines a simple logging abstraction used across layers.
- **DebounceHelper** — Provides debounced execution for frequent events such as progress updates.
- **StringExtensions** — Provides shared string helper methods such as folder-name cleanup.
