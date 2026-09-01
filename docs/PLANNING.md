# Local Netflix-Style Media Player — Planning Document

A WPF desktop application using **LibVLCSharp** to browse and play a local video library organized as `Videos\Show\Season\Episode` folders, presented in a Netflix-style browsing UI.

---

## 1. Overall Architecture

The application follows an **MVVM** architecture layered as follows:

- **Presentation Layer (WPF Views)** — XAML windows/pages/user controls rendering the Netflix-style UI (library grid, show detail page, player page).
- **ViewModel Layer** — Exposes observable collections of Shows/Seasons/Episodes, playback state, and commands (Play, Pause, Stop, Resume). Talks only to services/models, never to the filesystem or VLC directly.
- **Service Layer**:
  - **Library Scanner Service** — Walks the root Videos folder and builds an in-memory library model.
  - **Metadata Service** — Resolves poster images (local files or online, e.g., TMDB) and show/episode descriptions.
  - **Thumbnail Service** — Extracts frame thumbnails from video files using LibVLCSharp.
  - **Playback Service** — Wraps LibVLCSharp `MediaPlayer` and exposes play/pause/stop/seek and position events.
  - **Progress Store Service** — Persists and reloads watch progress to/from a local JSON file.
- **Model Layer** — Plain data classes: `Show`, `Season`, `Episode`, `WatchProgress`.
- **Data Layer** — JSON file(s) on disk for progress and cached metadata; no database required for a first version.

Data flows one direction on startup: Scanner ? Library Model ? Metadata/Thumbnail enrichment ? ViewModels ? Views. Playback events flow back: Player ? Playback Service ? Progress Store ? ViewModel (updates progress bars in UI).

---

## 2. Recommended Project Structure

```
MediaPlayerApp/
??? MediaPlayerApp.sln
??? src/
?   ??? MediaPlayerApp/                     (WPF app project)
?   ?   ??? App.xaml / App.xaml.cs
?   ?   ??? Views/
?   ?   ?   ??? LibraryView.xaml            (Netflix-style grid of shows)
?   ?   ?   ??? ShowDetailView.xaml         (seasons/episodes grid)
?   ?   ?   ??? PlayerView.xaml             (VideoView + controls overlay)
?   ?   ?   ??? ContinueWatchingView.xaml   (row of in-progress items)
?   ?   ??? ViewModels/
?   ?   ?   ??? LibraryViewModel.cs
?   ?   ?   ??? ShowDetailViewModel.cs
?   ?   ?   ??? PlayerViewModel.cs
?   ?   ?   ??? ContinueWatchingViewModel.cs
?   ?   ??? Controls/
?   ?   ?   ??? ProgressBarThumbnailControl.xaml   (poster + progress overlay)
?   ?   ??? Converters/                     (bool/visibility/time converters)
?   ?   ??? Resources/                      (styles, templates, icons)
?   ??? MediaPlayerApp.Core/                (class library, no WPF dependency)
?   ?   ??? Models/
?   ?   ?   ??? Show.cs
?   ?   ?   ??? Season.cs
?   ?   ?   ??? Episode.cs
?   ?   ?   ??? WatchProgress.cs
?   ?   ??? Services/
?   ?   ?   ??? LibraryScannerService.cs
?   ?   ?   ??? FileNameParser.cs
?   ?   ?   ??? MetadataService.cs
?   ?   ?   ??? ThumbnailService.cs
?   ?   ?   ??? PlaybackService.cs
?   ?   ?   ??? ProgressStoreService.cs
?   ?   ??? Interfaces/                     (service abstractions for DI/testing)
?   ??? MediaPlayerApp.Tests/               (unit tests for parser, scanner, progress store)
??? data/
?   ??? progress.json                       (generated at runtime)
?   ??? thumbnails/                         (cached generated thumbnails)
??? docs/
    ??? PLANNING.md
```

---

## 3. Scanning Folders and Parsing Season/Episode Numbers

### Folder scanning strategy
1. Accept a root **Videos** folder path (configurable in app settings).
2. Recursively enumerate directories:
   - **Depth 1** (directly under root) ? treated as a **Show** (folder name = show title, may need cleanup of dots/underscores into spaces).
   - **Depth 2** (subfolder of a show) ? treated as a **Season** folder (e.g., `Season 25`, `S25`).
   - **Files within season folders** ? treated as **Episode** files, filtered by known video extensions (`.mp4`, `.mkv`, `.avi`, `.mov`, etc.).
3. If a show folder contains video files directly (no season subfolder), treat them as a virtual "Season 1" or "Specials" bucket.
4. Build the in-memory tree: `Show ? List<Season> ? List<Episode>`.

### Filename parsing (e.g., `Show.Name.S25E01.mkv`)
- Use a regular expression to extract season/episode tokens, tolerant of separators (dots, spaces, underscores, hyphens):
  - Pattern concept: locate `S(\d{1,3})E(\d{1,3})` (case-insensitive) anywhere in the filename.
  - Optionally support multi-episode files (`S01E01E02`) and alternate formats (`1x01`) as a fallback pattern.
- **Show name extraction**:
  - Primary source: the top-level folder name (cleaned: replace dots/underscores with spaces, trim release-group tags in brackets).
  - Fallback: if folder name is generic, derive show name from the filename portion preceding the `SxxEyy` token.
- **Episode title**: text following the `SxxEyy` token, before the file extension or quality tags (`1080p`, `WEB-DL`, etc.), cleaned similarly.
- Encapsulate all parsing logic in a single `FileNameParser` utility so it can be unit tested independently of the filesystem.
- Log/flag files that fail to match the expected pattern so they can be shown in an "Unrecognized Files" section rather than silently dropped.

---

## 4. Integrating LibVLCSharp for Playback

- Reference the `LibVLCSharp` and `LibVLCSharp.WPF` NuGet packages, plus the appropriate `VideoLAN.LibVLC.Windows` native package.
- Initialize the LibVLC engine **once** at application startup (a singleton `LibVLC` instance shared across the app) to avoid repeated costly initialization.
- Host playback using the `VideoView` control (from `LibVLCSharp.WPF`) placed in `PlayerView.xaml`; bind its `MediaPlayer` property to a `MediaPlayer` instance owned by the Playback Service.
- Playback Service responsibilities:
  - Create a `Media` object from the selected episode's file path.
  - Expose `Play()`, `Pause()`, `Stop()`, `SeekTo(TimeSpan)`.
  - Subscribe to `MediaPlayer` events (`TimeChanged`, `EndReached`, `Playing`, `Paused`, `Stopped`) and re-publish them as .NET events/observables consumed by `PlayerViewModel`.
  - On start, if stored progress exists for the episode, seek to the saved position (unless the episode was already marked complete).
- Ensure the `LibVLC`/`MediaPlayer` instances are disposed on window close/app exit to release native resources cleanly.
- Provide standard transport controls (play/pause toggle, seek slider bound to `Position`, volume slider, fullscreen toggle) in the player overlay.

---

## 5. Generating Thumbnails from Video Frames

- Use LibVLCSharp's offline/snapshot capability to extract a representative frame per episode:
  - Create a lightweight, headless `MediaPlayer` (not attached to any visible `VideoView`) purely for snapshot generation.
  - Load the episode's media, seek to a fixed point (e.g., 10% or a fixed timestamp like 2 minutes in, to skip intros/black frames), and trigger a snapshot to a temp/cache file.
  - Save the resulting image into a `thumbnails/` cache folder, named by a stable key (e.g., hash of the episode's file path) so it is not regenerated every launch.
- Run thumbnail generation **asynchronously in the background** after the initial library scan completes, so the UI can populate progressively (placeholder image shown until the real thumbnail is ready).
- Cache invalidation: regenerate a thumbnail only if the source video file's last-modified timestamp changed or the cached thumbnail is missing.
- Fall back to a generic placeholder image if snapshot generation fails (e.g., corrupt file, unsupported codec).

---

## 6. Poster Images: Local Files and Optional Online Metadata (TMDB)

### Local poster resolution
- For each Show folder, look for a poster image using common conventions: `poster.jpg`, `poster.png`, `folder.jpg`, or the first image file found in the folder.
- Similarly check Season folders for season-specific posters if present.
- If found, use these directly and skip any network calls for that item.

### Optional online metadata (TMDB) as fallback
- Only invoked when no local poster/metadata exists, and only if the user enables an "online metadata" setting (respect offline-first design and privacy).
- Metadata Service responsibilities:
  - Search TMDB by cleaned show name to find the best match (and confirm via year if derivable from folder name).
  - Download and cache the show poster, backdrop, and description locally (in a metadata cache folder), so subsequent launches don't re-query the network.
  - Optionally fetch episode-level metadata (title, overview, air date) to enrich or correct parsed filename data.
- Store an on-disk cache manifest (e.g., `metadata-cache.json`) mapping show identifiers to cached asset paths and last-refreshed timestamps, with a manual "Refresh Metadata" action for the user.
- All network calls must be resilient: timeouts, retries with backoff, and graceful fallback to placeholder/generic artwork on failure.

---

## 7. Tracking Playback Progress (Play/Pause/Stop) and JSON Storage

- Define a `WatchProgress` model per episode: file path/unique key, last position (`TimeSpan`), total duration, `IsCompleted` flag, and `LastWatchedUtc` timestamp.
- **Progress Store Service**:
  - Loads `progress.json` (a dictionary keyed by episode identifier) into memory at startup.
  - Persists changes to disk **debounced** (e.g., every few seconds during playback or on pause/stop events) rather than on every single time-tick, to avoid excessive disk I/O.
  - Writes are performed via a temp-file-then-atomic-rename pattern to avoid corrupting the JSON on crash/power loss.
- **Update triggers**:
  - On `TimeChanged`: update in-memory position (throttled).
  - On `Paused`/`Stopped`/window-close: force an immediate flush to disk.
  - On `EndReached` or position ? ~95% of duration: mark `IsCompleted = true` and clear/adjust "Continue Watching" eligibility.
- Provide a serialization contract (e.g., `System.Text.Json`) with versioning (a top-level schema version field) to allow safe future migrations of the progress file format.

---

## 8. Netflix-Style UI: Grid Layout, Thumbnails, Titles, Progress Bars

- **Library View**: an `ItemsControl`/`ListBox` using a `WrapPanel` or `UniformGrid` as its `ItemsPanel`, displaying one card per Show (poster + title), styled with dark background, rounded corners, and a subtle hover-scale animation for a Netflix feel.
- **Show Detail View**: displays seasons as a horizontal selector (tabs or dropdown) and episodes as a grid/list of cards, each showing the generated thumbnail, episode number/title, and a short progress bar overlay if partially watched.
- **Reusable "Media Card" control**: composed of poster/thumbnail image, gradient overlay for text legibility, title text, and an optional bottom progress bar (a thin `ProgressBar` or custom `Rectangle` whose width is bound to `WatchedFraction`).
- **Row-based layout**: group content into horizontally scrollable rows (e.g., "All Shows", "Continue Watching", "Recently Added") using nested horizontally-scrolling `ItemsControl`s, mirroring Netflix's row/carousel pattern.
- Use a dark theme (colors, fonts) with consistent styles defined as WPF `Style`/`ControlTemplate` resources in a shared resource dictionary for visual consistency and easy theming.
- Clicking a Show card navigates to Show Detail; clicking an Episode card navigates to/starts the Player View, optionally resuming from stored progress.

---

## 9. "Continue Watching" Section Based on Stored Progress

- On library load, after scanning and after progress data is loaded, compute a **Continue Watching** list:
  - Include episodes where `0 < WatchedFraction < CompletionThreshold` (e.g., between 2% and 95% watched).
  - Exclude episodes marked `IsCompleted`.
  - Sort by `LastWatchedUtc` descending (most recently watched first).
  - Optionally cap the list to a fixed number of items (e.g., top 10–20) for UI performance.
- Expose this as a dedicated `ContinueWatchingViewModel`/observable collection, rendered as the first row on the Library View for quick access.
- Each Continue Watching card reuses the Media Card control, showing the episode thumbnail (not the show poster) plus the progress bar, and clicking it resumes playback directly at the stored position.
- The collection must update reactively: when playback stops/pauses and progress is persisted, refresh (add/reorder/remove) the corresponding entry without requiring a full library rescan.
